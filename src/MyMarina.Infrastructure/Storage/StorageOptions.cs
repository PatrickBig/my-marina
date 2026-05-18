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
    /// Signing region sent to the S3-compatible provider. Cloudflare R2 requires "auto";
    /// AWS S3 requires the bucket's region (e.g. "us-east-1"); LocalStack accepts anything.
    /// Leave empty to let the AWS SDK use its default.
    /// </summary>
    public string Region { get; set; } = string.Empty;
    /// <summary>
    /// Optional override for the host used in presigned upload URLs returned to the browser.
    /// Set this when the API reaches S3 via an internal hostname (e.g. "http://localstack:4566" in Docker)
    /// but browsers must use a different address (e.g. "http://localhost:4566").
    /// Leave empty for cloud providers where the same endpoint is reachable from everywhere.
    /// </summary>
    public string PublicEndpoint { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketPublicBaseUrl { get; set; } = string.Empty;
    public int UploadTtlMinutes { get; set; } = 15;
    public long MaxFileSizeBytes { get; set; } = 20_971_520;
}
