using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace MyMarina.Infrastructure.Storage;

public sealed class S3StorageProvider : IStorageProvider, IDisposable
{
    private readonly AmazonS3Client _client;
    private readonly S3Options _opts;

    public S3StorageProvider(IOptions<StorageOptions> options)
    {
        _opts = options.Value.S3;
        var config = new AmazonS3Config
        {
            ServiceURL = _opts.Endpoint,
            ForcePathStyle = true,
        };
        if (!string.IsNullOrEmpty(_opts.Region))
            config.AuthenticationRegion = _opts.Region;
        _client = new AmazonS3Client(
            new BasicAWSCredentials(_opts.AccessKey, _opts.SecretKey),
            config);
    }

    public string GetPublicUrl(string key)
        => $"{_opts.BucketPublicBaseUrl.TrimEnd('/')}/{key}";

    public async Task PutObjectStreamAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _opts.Bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
            UseChunkEncoding = false,
        };
        await _client.PutObjectAsync(request, ct);
    }

    public async Task<Stream?> GetObjectStreamAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.GetObjectAsync(_opts.Bucket, key, ct);
            var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms, ct);
            ms.Position = 0;
            return ms;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _client.DeleteObjectAsync(_opts.Bucket, key, ct);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchKey")
        {
            // Idempotent — object already gone
        }
    }

    public async Task DeleteByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        string? continuationToken = null;
        do
        {
            var listReq = new ListObjectsV2Request
            {
                BucketName = _opts.Bucket,
                Prefix = prefix,
                ContinuationToken = continuationToken,
            };
            var listResp = await _client.ListObjectsV2Async(listReq, ct);

            if (listResp.S3Objects.Count == 0) break;

            var deleteReq = new DeleteObjectsRequest
            {
                BucketName = _opts.Bucket,
                Objects = listResp.S3Objects
                    .Select(o => new KeyVersion { Key = o.Key })
                    .ToList(),
            };
            await _client.DeleteObjectsAsync(deleteReq, ct);

            continuationToken = listResp.IsTruncated ? listResp.NextContinuationToken : null;
        }
        while (continuationToken is not null);
    }

    public void Dispose() => _client.Dispose();
}
