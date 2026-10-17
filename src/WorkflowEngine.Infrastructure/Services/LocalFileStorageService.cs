using Microsoft.Extensions.Hosting;
using WorkflowEngine.Application.Common.Exceptions;
using WorkflowEngine.Application.Interfaces;

namespace WorkflowEngine.Infrastructure.Services;

/// <summary>
/// Layanan penyimpanan file attachment berbasis disk lokal.
/// Memastikan isolasi nama file (GUID) dan sanitasi path traversal.
/// </summary>
public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _baseStoragePath;

    public LocalFileStorageService(IHostEnvironment env)
    {
        _baseStoragePath = Path.Combine(env.ContentRootPath, "Storage", "Attachments");
        if (!Directory.Exists(_baseStoragePath))
        {
            Directory.CreateDirectory(_baseStoragePath);
        }
    }

    public async Task<(string storagePath, long fileSizeBytes)> SaveFileAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        var safeExt = Path.GetExtension(fileName).ToLowerInvariant();
        var uniqueFileName = $"{Guid.NewGuid():N}{safeExt}";
        var relativePath = Path.Combine(DateTime.UtcNow.ToString("yyyyMM"), uniqueFileName);
        var fullPath = Path.Combine(_baseStoragePath, relativePath);

        var dir = Path.GetDirectoryName(fullPath)!;
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await using var output = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await fileStream.CopyToAsync(output, ct);
        var sizeBytes = output.Length;

        return (relativePath, sizeBytes);
    }

    public Task<(Stream stream, string contentType, string fileName)> GetFileAsync(string storagePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_baseStoragePath, storagePath);
        if (!File.Exists(fullPath))
        {
            throw new NotFoundException("Berkas Attachment", storagePath);
        }

        var ext = Path.GetExtension(storagePath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(((Stream)stream, contentType, Path.GetFileName(fullPath)));
    }

    public Task DeleteFileAsync(string storagePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_baseStoragePath, storagePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }
}
