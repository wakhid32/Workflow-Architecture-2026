namespace UserManagement.Infrastructure.Keycloak;

/// <summary>
/// Options Pattern configuration untuk koneksi Keycloak.
/// Dibaca dari section "Keycloak" di appsettings.json.
/// </summary>
public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    /// <summary>Nama realm Keycloak (misalnya "EnterpriseRealm").</summary>
    public string Realm { get; set; } = "EnterpriseRealm";

    /// <summary>Base URL server Keycloak (misalnya "http://localhost:8080/").</summary>
    public string AuthServerUrl { get; set; } = "http://localhost:8080/";

    /// <summary>Client ID untuk backend API (OIDC Confidential Client).</summary>
    public string ClientId { get; set; } = "workflow-backend-api";

    /// <summary>Client Secret untuk backend API.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Username admin Keycloak untuk Admin REST API.</summary>
    public string AdminUsername { get; set; } = "admin";

    /// <summary>Password admin Keycloak untuk Admin REST API.</summary>
    public string AdminPassword { get; set; } = "admin";

    // ─── Computed Properties ─────────────────────────────────────────────────

    /// <summary>Token Endpoint: POST /realms/{realm}/protocol/openid-connect/token</summary>
    public string TokenEndpoint => $"{AuthServerUrl.TrimEnd('/')}/realms/{Realm}/protocol/openid-connect/token";

    /// <summary>Admin Token Endpoint: POST /realms/master/protocol/openid-connect/token</summary>
    public string AdminTokenEndpoint => $"{AuthServerUrl.TrimEnd('/')}/realms/master/protocol/openid-connect/token";

    /// <summary>Admin Users Endpoint: /admin/realms/{realm}/users</summary>
    public string AdminUsersEndpoint => $"{AuthServerUrl.TrimEnd('/')}/admin/realms/{Realm}/users";

    /// <summary>JWKS Endpoint: /realms/{realm}/protocol/openid-connect/certs</summary>
    public string JwksEndpoint => $"{AuthServerUrl.TrimEnd('/')}/realms/{Realm}/protocol/openid-connect/certs";

    /// <summary>OpenID Connect Discovery URL.</summary>
    public string MetadataAddress => $"{AuthServerUrl.TrimEnd('/')}/realms/{Realm}/.well-known/openid-configuration";

    /// <summary>Issuer yang diharapkan dalam JWT.</summary>
    public string ValidIssuer => $"{AuthServerUrl.TrimEnd('/')}/realms/{Realm}";
}
