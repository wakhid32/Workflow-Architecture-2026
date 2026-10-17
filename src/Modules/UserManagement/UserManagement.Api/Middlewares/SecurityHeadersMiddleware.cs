using Microsoft.AspNetCore.Http;

namespace UserManagement.Api.Middlewares;

/// <summary>
/// Middleware penambahan OWASP Security HTTP Headers.
/// Setiap response HTTP otomatis menyertakan header keamanan standar
/// sesuai OWASP Security Headers dan SECURITY.md Section 8.3.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // HSTS: Force HTTPS for 1 year
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";

            // Prevent MIME type sniffing
            headers["X-Content-Type-Options"] = "nosniff";

            // Prevent clickjacking
            headers["X-Frame-Options"] = "DENY";

            // Control referrer information
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Content Security Policy
            headers["Content-Security-Policy"] = "default-src 'self'; frame-ancestors 'none';";

            // Permissions Policy (restrict browser features)
            headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";

            // Remove server header (information disclosure prevention)
            headers.Remove("Server");
            headers.Remove("X-Powered-By");

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
