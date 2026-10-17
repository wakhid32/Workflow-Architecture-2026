using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Application.Common.Exceptions;

namespace WorkflowEngine.WebApi.Filters;

/// <summary>
/// Global Exception Filter untuk menangani exception bisnis workflow
/// dan menerjemahkannya ke HTTP Status Code terstandarisasi (RFC 7807 Problem Details).
/// </summary>
public sealed class ApiExceptionFilterAttribute : ExceptionFilterAttribute
{
    private readonly ILogger<ApiExceptionFilterAttribute> _logger;

    public ApiExceptionFilterAttribute(ILogger<ApiExceptionFilterAttribute> logger)
    {
        _logger = logger;
    }

    public override void OnException(ExceptionContext context)
    {
        HandleCustomException(context);
        base.OnException(context);
    }

    private void HandleCustomException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case WorkflowException ex:
                _logger.LogWarning(ex, "Workflow business logic exception occurred.");
                context.Result = new ObjectResult(new
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Aturan Bisnis Workflow Dilanggar",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = ex.Message,
                    Instance = context.HttpContext.Request.Path
                })
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
                context.ExceptionHandled = true;
                break;

            case NotFoundException ex:
                _logger.LogWarning(ex, "Entity not found exception occurred.");
                context.Result = new ObjectResult(new
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Title = "Data Tidak Ditemukan",
                    Status = StatusCodes.Status404NotFound,
                    Detail = ex.Message,
                    Instance = context.HttpContext.Request.Path
                })
                {
                    StatusCode = StatusCodes.Status404NotFound
                };
                context.ExceptionHandled = true;
                break;

            case ConcurrencyException ex:
            case DbUpdateConcurrencyException:
                _logger.LogWarning(context.Exception, "Concurrency conflict occurred.");
                context.Result = new ObjectResult(new
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                    Title = "Konflik Data Konkurensi (Race Condition)",
                    Status = StatusCodes.Status409Conflict,
                    Detail = "Dokumen ini telah direspon oleh rekan lain atau telah diperbarui pada sesi lain. Halaman Anda telah kedaluwarsa, silakan muat ulang (refresh) halaman untuk melihat data terbaru.",
                    Instance = context.HttpContext.Request.Path
                })
                {
                    StatusCode = StatusCodes.Status409Conflict
                };
                context.ExceptionHandled = true;
                break;

            case ForbiddenException ex:
                _logger.LogWarning(ex, "Forbidden workflow access attempted.");
                context.Result = new ObjectResult(new
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                    Title = "Akses Ditolak",
                    Status = StatusCodes.Status403Forbidden,
                    Detail = ex.Message,
                    Instance = context.HttpContext.Request.Path
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                context.ExceptionHandled = true;
                break;

            default:
                _logger.LogError(context.Exception, "Unhandled exception occurred.");
                context.Result = new ObjectResult(new
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Title = "Terjadi Kesalahan Server Internal",
                    Status = StatusCodes.Status500InternalServerError,
                    Detail = context.Exception.Message,
                    Instance = context.HttpContext.Request.Path
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
                context.ExceptionHandled = true;
                break;
        }
    }
}
