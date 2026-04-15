namespace MyMarina.Application.Abstractions;

/// <summary>
/// Abstracts file storage — swap local → Azure Blob → S3 without
/// touching application code.
/// </summary>
public interface IStorageService
{
    Task<string> UploadAsync(string containerName, string fileName, Stream content, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string containerName, string fileName, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string containerName, string fileName, CancellationToken ct = default);
}
