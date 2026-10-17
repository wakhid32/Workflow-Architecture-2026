using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserManagement.Application.Interfaces;

namespace UserManagement.Infrastructure.Keycloak;

/// <summary>
/// Implementasi Keycloak Admin REST API service.
/// Menangani CRUD user, role management, dan session control
/// via /admin/realms/{realm}/users endpoints.
/// </summary>
public sealed class KeycloakAdminService : IKeycloakAdminService
{
    private readonly KeycloakHttpClient _keycloakClient;
    private readonly HttpClient _httpClient;
    private readonly KeycloakOptions _options;
    private readonly ILogger<KeycloakAdminService> _logger;

    public KeycloakAdminService(
        KeycloakHttpClient keycloakClient,
        HttpClient httpClient,
        IOptions<KeycloakOptions> options,
        ILogger<KeycloakAdminService> logger)
    {
        _keycloakClient = keycloakClient;
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> CreateUserAsync(CreateKeycloakUserRequest request, CancellationToken ct)
    {
        var adminToken = await _keycloakClient.GetAdminAccessTokenAsync(ct);

        var payload = new
        {
            username = request.Username,
            email = request.Email,
            firstName = request.FirstName,
            lastName = request.LastName,
            enabled = request.Enabled,
            emailVerified = true,
            credentials = new[]
            {
                new { type = "password", value = request.Password, temporary = false }
            }
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.AdminUsersEndpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.SendAsync(httpRequest, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Gagal membuat user Keycloak: {StatusCode} - {Body}", response.StatusCode, errorBody);
            throw new InvalidOperationException($"Gagal membuat user Keycloak: {response.StatusCode}. {errorBody}");
        }

        // Extract User ID from Location header
        var locationHeader = response.Headers.Location?.ToString();
        if (string.IsNullOrEmpty(locationHeader))
            throw new InvalidOperationException("Keycloak tidak mengembalikan Location header setelah pembuatan user.");

        var userId = locationHeader.Split('/').Last();
        _logger.LogInformation("User Keycloak berhasil dibuat: {UserId}", userId);

        return userId;
    }

    public async Task AssignRealmRoleAsync(string keycloakUserId, string roleName, CancellationToken ct)
    {
        var adminToken = await _keycloakClient.GetAdminAccessTokenAsync(ct);

        // Step 1: Get the role representation
        var rolesUrl = $"{_options.AuthServerUrl.TrimEnd('/')}/admin/realms/{_options.Realm}/roles/{roleName}";
        var roleRequest = new HttpRequestMessage(HttpMethod.Get, rolesUrl);
        roleRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var roleResponse = await _httpClient.SendAsync(roleRequest, ct);

        if (!roleResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Role '{RoleName}' tidak ditemukan di Keycloak. Skipping assignment.", roleName);
            return;
        }

        var roleJson = await roleResponse.Content.ReadAsStringAsync(ct);

        // Step 2: Assign role to user
        var assignUrl = $"{_options.AdminUsersEndpoint}/{keycloakUserId}/role-mappings/realm";
        var assignRequest = new HttpRequestMessage(HttpMethod.Post, assignUrl)
        {
            Content = new StringContent($"[{roleJson}]", Encoding.UTF8, "application/json")
        };
        assignRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var assignResponse = await _httpClient.SendAsync(assignRequest, ct);
        assignResponse.EnsureSuccessStatusCode();

        _logger.LogInformation("Role '{RoleName}' assigned ke user {UserId}", roleName, keycloakUserId);
    }

    public async Task DisableUserAsync(string keycloakUserId, CancellationToken ct)
    {
        await UpdateUserEnabledStatusAsync(keycloakUserId, false, ct);
        _logger.LogInformation("User Keycloak dinonaktifkan: {UserId}", keycloakUserId);
    }

    public async Task EnableUserAsync(string keycloakUserId, CancellationToken ct)
    {
        await UpdateUserEnabledStatusAsync(keycloakUserId, true, ct);
        _logger.LogInformation("User Keycloak diaktifkan kembali: {UserId}", keycloakUserId);
    }

    public async Task LogoutAllSessionsAsync(string keycloakUserId, CancellationToken ct)
    {
        var adminToken = await _keycloakClient.GetAdminAccessTokenAsync(ct);
        var url = $"{_options.AdminUsersEndpoint}/{keycloakUserId}/logout";

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Semua sesi Keycloak dihapus untuk user: {UserId}", keycloakUserId);
    }

    public async Task<KeycloakUserDto?> GetUserByIdAsync(string keycloakUserId, CancellationToken ct)
    {
        var adminToken = await _keycloakClient.GetAdminAccessTokenAsync(ct);
        var url = $"{_options.AdminUsersEndpoint}/{keycloakUserId}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return null;

        var userData = await response.Content.ReadFromJsonAsync<KeycloakUserJsonResponse>(cancellationToken: ct);
        if (userData is null)
            return null;

        // Get realm roles
        var rolesUrl = $"{_options.AdminUsersEndpoint}/{keycloakUserId}/role-mappings/realm";
        var rolesRequest = new HttpRequestMessage(HttpMethod.Get, rolesUrl);
        rolesRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var rolesResponse = await _httpClient.SendAsync(rolesRequest, ct);
        var roles = new List<string>();

        if (rolesResponse.IsSuccessStatusCode)
        {
            var rolesData = await rolesResponse.Content
                .ReadFromJsonAsync<List<KeycloakRoleJsonResponse>>(cancellationToken: ct);
            roles = rolesData?.Select(r => r.Name).Where(n => !string.IsNullOrEmpty(n)).ToList() ?? [];
        }

        return new KeycloakUserDto(
            userData.Id,
            userData.Username,
            userData.Email ?? string.Empty,
            userData.FirstName,
            userData.LastName,
            userData.Enabled,
            roles.AsReadOnly());
    }

    public async Task SendPasswordResetEmailAsync(string keycloakUserId, CancellationToken ct)
    {
        var adminToken = await _keycloakClient.GetAdminAccessTokenAsync(ct);
        var url = $"{_options.AdminUsersEndpoint}/{keycloakUserId}/execute-actions-email";

        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new StringContent("[\"UPDATE_PASSWORD\"]", Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Email reset password dikirim ke user: {UserId}", keycloakUserId);
    }

    // ─── Private Helpers ─────────────────────────────────────────────────────

    private async Task UpdateUserEnabledStatusAsync(string keycloakUserId, bool enabled, CancellationToken ct)
    {
        var adminToken = await _keycloakClient.GetAdminAccessTokenAsync(ct);
        var url = $"{_options.AdminUsersEndpoint}/{keycloakUserId}";

        var payload = new { enabled };
        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    // ─── JSON Response Models ────────────────────────────────────────────────

    private sealed class KeycloakUserJsonResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }
    }

    private sealed class KeycloakRoleJsonResponse
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
