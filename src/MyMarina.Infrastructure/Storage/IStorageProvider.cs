namespace MyMarina.Infrastructure.Storage;

public interface IStorageProvider
{
    Task<UploadTicket> CreateUploadTicketAsync(string key, string contentType, long maxBytes, TimeSpan ttl, CancellationToken ct = default);
    Task<StoredFileInfo> ConfirmUploadAsync(string key, CancellationToken ct = default);
    string GetPublicUrl(string key);
    Task DeleteAsync(string key, CancellationToken ct = default);
    Task DeleteByPrefixAsync(string prefix, CancellationToken ct = default);
    Task<Stream?> GetObjectStreamAsync(string key, CancellationToken ct = default);
    Task PutObjectStreamAsync(string key, Stream content, string contentType, CancellationToken ct = default);
}
