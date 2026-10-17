namespace WorkflowEngine.Application.Interfaces;

/// <summary>
/// Layanan penyimpanan berkas lampiran pendukung disposisi.
/// </summary>
public interface IFileStorageService
{
    Task<(string storagePath, long fileSizeBytes)> SaveFileAsync(Stream fileStream, string fileName, CancellationToken ct = default);
    Task<(Stream stream, string contentType, string fileName)> GetFileAsync(string storagePath, CancellationToken ct = default);
    Task DeleteFileAsync(string storagePath, CancellationToken ct = default);
}
