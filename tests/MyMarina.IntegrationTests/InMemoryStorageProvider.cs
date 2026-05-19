using System.Collections.Concurrent;
using MyMarina.Infrastructure.Storage;

namespace MyMarina.IntegrationTests;

/// <summary>
/// Test-only storage provider backed by an in-memory dictionary.
/// PutObjectStreamAsync stores bytes so GetObjectStreamAsync (used by ImageVariantGenerationJob) works.
/// </summary>
public sealed class InMemoryStorageProvider : IStorageProvider
{
    public static readonly ConcurrentDictionary<string, (byte[] Data, string ContentType)> Objects = new();

    public string GetPublicUrl(string key) => $"http://localhost/test-storage/{key}";

    public Task PutObjectStreamAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        content.CopyTo(ms);
        Objects[key] = (ms.ToArray(), contentType);
        return Task.CompletedTask;
    }

    public Task<Stream?> GetObjectStreamAsync(string key, CancellationToken ct = default)
    {
        if (!Objects.TryGetValue(key, out var obj))
            return Task.FromResult<Stream?>(null);
        return Task.FromResult<Stream?>(new MemoryStream(obj.Data));
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        Objects.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task DeleteByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        foreach (var k in Objects.Keys.Where(k => k.StartsWith(prefix)).ToList())
            Objects.TryRemove(k, out _);
        return Task.CompletedTask;
    }
}
