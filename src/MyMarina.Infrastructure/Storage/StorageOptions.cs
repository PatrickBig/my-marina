namespace MyMarina.Infrastructure.Storage;

public sealed class StorageOptions
{
    public string Provider { get; set; } = string.Empty;
    public S3Options S3 { get; set; } = new();
}

public sealed class S3Options
{
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>
    /// Signing region for the S3-compatible provider. Cloudflare R2 requires "auto";
    /// AWS S3 requires the bucket's region (e.g. "us-east-1"). Leave empty for LocalStack/RustFS.
    /// </summary>
    public string Region { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketPublicBaseUrl { get; set; } = string.Empty;
    public long MaxFileSizeBytes { get; set; } = 20_971_520;
}
