using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserManagement.Application.Services;

namespace UserManagement.Infrastructure.Keycloak;

/// <summary>
/// HTTP client untuk komunikasi dengan Keycloak Token & Admin REST API.
/// Dilindungi oleh Polly Resilience Pipeline (Circuit Breaker + Retry + Timeout).
/// </summary>
public sealed class KeycloakHttpClient : IKeycloakHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly KeycloakOptions _options;
    private readonly ILogger<KeycloakHttpClient> _logger;

    // Admin token cache (short-lived, refreshed when expired)
    private string? _adminAccessToken;
    private DateTime _adminTokenExpiry = DateTime.MinValue;
    private readonly SemaphoreSlim _adminTokenLock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public KeycloakHttpClient(
        HttpClient httpClient,
        IOptions<KeycloakOptions> options,
        ILogger<KeycloakHttpClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Resource Owner Password Credentials (Direct Grant) flow.
    /// </summary>
    public async Task<KeycloakTokenResponse> RequestTokenAsync(string username, string password, CancellationToken ct)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["username"] = username,
            ["password"] = password,
            ["scope"] = "openid profile email"
        });

        var response = await _httpClient.PostAsync(_options.TokenEndpoint, content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Keycloak login gagal: StatusCode={StatusCode}, Body={Body}",
                response.StatusCode, errorBody);
            throw new InvalidOperationException("Username atau password tidak valid.");
        }

        var tokenData = await response.Content.ReadFromJsonAsync<KeycloakTokenJsonResponse>(JsonOptions, ct)
            ?? throw new InvalidOperationException("Gagal memparse respons token dari Keycloak.");

        return new KeycloakTokenResponse(
            tokenData.AccessToken,
            tokenData.RefreshToken,
            tokenData.ExpiresIn,
            tokenData.RefreshExpiresIn);
    }

    /// <summary>
    /// Refresh token grant flow.
    /// </summary>
    public async Task<KeycloakTokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["refresh_token"] = refreshToken
        });

        var response = await _httpClient.PostAsync(_options.TokenEndpoint, content, ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Refresh token tidak valid atau sudah kedaluwarsa.");
        }

        var tokenData = await response.Content.ReadFromJsonAsync<KeycloakTokenJsonResponse>(JsonOptions, ct)
            ?? throw new InvalidOperationException("Gagal memparse respons refresh token dari Keycloak.");

        return new KeycloakTokenResponse(
            tokenData.AccessToken,
            tokenData.RefreshToken,
            tokenData.ExpiresIn,
            tokenData.RefreshExpiresIn);
    }

    /// <summary>
    /// Revokasi refresh token di Keycloak.
    /// </summary>
    public async Task RevokeTokenAsync(string token, CancellationToken ct)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["token"] = token,
            ["token_type_hint"] = "refresh_token"
        });

        var revokeUrl = _options.TokenEndpoint.Replace("/token", "/revoke");
        await _httpClient.PostAsync(revokeUrl, content, ct);
    }

    /// <summary>
    /// Update password user via Admin REST API.
    /// </summary>
    public async Task UpdatePasswordAsync(string keycloakUserId, string newPassword, CancellationToken ct)
    {
        var adminToken = await GetAdminAccessTokenAsync(ct);
        var url = $"{_options.AdminUsersEndpoint}/{keycloakUserId}/reset-password";

        var payload = new
        {
            type = "password",
            value = newPassword,
            temporary = false
        };

        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Mendapatkan admin access token untuk Admin REST API.
    /// Di-cache secara internal dan di-refresh sebelum kedaluwarsa.
    /// </summary>
    internal async Task<string> GetAdminAccessTokenAsync(CancellationToken ct)
    {
        if (_adminAccessToken is not null && _adminTokenExpiry > DateTime.UtcNow.AddSeconds(30))
            return _adminAccessToken;

        await _adminTokenLock.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock
            if (_adminAccessToken is not null && _adminTokenExpiry > DateTime.UtcNow.AddSeconds(30))
                return _adminAccessToken;

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = "admin-cli",
                ["username"] = _options.AdminUsername,
                ["password"] = _options.AdminPassword
            });

            var response = await _httpClient.PostAsync(_options.AdminTokenEndpoint, content, ct);
            response.EnsureSuccessStatusCode();

            var tokenData = await response.Content.ReadFromJsonAsync<KeycloakTokenJsonResponse>(JsonOptions, ct)
                ?? throw new InvalidOperationException("Gagal mendapatkan admin token Keycloak.");

            _adminAccessToken = tokenData.AccessToken;
            _adminTokenExpiry = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn);

            return _adminAccessToken;
        }
        finally
        {
            _adminTokenLock.Release();
        }
    }

    // ─── JSON Response Model ─────────────────────────────────────────────────

    private sealed class KeycloakTokenJsonResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("refresh_expires_in")]
        public int RefreshExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
    }
}
