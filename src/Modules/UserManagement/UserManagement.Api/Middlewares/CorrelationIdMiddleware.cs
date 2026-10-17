using Microsoft.AspNetCore.Http;

namespace UserManagement.Api.Middlewares;

/// <summary>
/// Middleware injeksi/forward W3C Trace Context (X-Correlation-ID).
/// Setiap request HTTP masuk mendapatkan atau meneruskan Correlation ID unik
/// yang tercatat pada response header, diagnostic context, dan audit log.
/// Sesuai SECURITY.md Section 10.2.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Gunakan Correlation ID dari header jika ada, atau buat baru
        if (!context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId) ||
            string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("D");
        }

        // Simpan di HttpContext.Items agar bisa diakses seluruh pipeline
        context.Items[CorrelationIdHeader] = correlationId.ToString();

        // Tambahkan ke response header
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId.ToString();
            return Task.CompletedTask;
        });

        await _next(context);
    }
}

/// <summary>
/// Extension untuk mengambil Correlation ID dari HttpContext.
/// </summary>
public static class CorrelationIdExtensions
{
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public static string GetCorrelationId(this HttpContext context)
    {
        if (context.Items.TryGetValue(CorrelationIdHeader, out var correlationId) &&
            correlationId is string id)
        {
            return id;
        }

        return Guid.NewGuid().ToString("D");
    }
}
