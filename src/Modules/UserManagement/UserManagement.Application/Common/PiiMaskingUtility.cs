using System.Text.RegularExpressions;

namespace UserManagement.Application.Common;

/// <summary>
/// Utilitas penyamaran data sensitif (PII) pada logging dan audit.
/// Sesuai amanat UU PDP / GDPR dan ISO 27001 — tidak boleh ada
/// data pribadi tersimpan dalam bentuk plain text di log.
/// </summary>
public static partial class PiiMaskingUtility
{
    /// <summary>
    /// Masking nomor handphone: 0812****5432
    /// </summary>
    public static string MaskPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length < 8)
            return "[REDACTED]";

        return string.Concat(
            phoneNumber.AsSpan(0, 4),
            new string('*', phoneNumber.Length - 8),
            phoneNumber.AsSpan(phoneNumber.Length - 4));
    }

    /// <summary>
    /// Masking email: b***@company.co.id
    /// </summary>
    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "[REDACTED]";

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return "[REDACTED]";

        var localPart = email[..atIndex];
        var domainPart = email[atIndex..];

        var maskedLocal = localPart.Length <= 1
            ? $"{localPart}***"
            : $"{localPart[0]}{new string('*', 3)}";

        return $"{maskedLocal}{domainPart}";
    }

    /// <summary>
    /// Menyanitasi string yang berpotensi mengandung token, password, atau secret.
    /// </summary>
    public static string SanitizePayload(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return string.Empty;

        var sanitized = PasswordPattern().Replace(payload, "\"password\":\"[REDACTED]\"");
        sanitized = TokenPattern().Replace(sanitized, "\"token\":\"[REDACTED]\"");
        sanitized = SecretPattern().Replace(sanitized, "\"secret\":\"[REDACTED]\"");
        sanitized = BearerPattern().Replace(sanitized, "Bearer [REDACTED]");

        return sanitized;
    }

    [GeneratedRegex("\"password\"\\s*:\\s*\"[^\"]*\"", RegexOptions.IgnoreCase)]
    private static partial Regex PasswordPattern();

    [GeneratedRegex("\"(?:access_?token|refresh_?token|token)\"\\s*:\\s*\"[^\"]*\"", RegexOptions.IgnoreCase)]
    private static partial Regex TokenPattern();

    [GeneratedRegex("\"(?:client_?secret|secret)\"\\s*:\\s*\"[^\"]*\"", RegexOptions.IgnoreCase)]
    private static partial Regex SecretPattern();

    [GeneratedRegex("Bearer\\s+[A-Za-z0-9\\-._~+/]+=*", RegexOptions.IgnoreCase)]
    private static partial Regex BearerPattern();
}
