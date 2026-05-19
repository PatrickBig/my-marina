namespace MyMarina.Infrastructure.Storage;

public interface IStorageProvider
{
    string GetPublicUrl(string key);
    Task PutObjectStreamAsync(string key, Stream content, string contentType, CancellationToken ct = default);
    Task<Stream?> GetObjectStreamAsync(string key, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
    Task DeleteByPrefixAsync(string prefix, CancellationToken ct = default);
}
