using MyMarina.Domain.Enums;

namespace MyMarina.Domain.Entities;

public class MarinaPhoto
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid TenantId { get; set; }
    public Guid MarinaId { get; set; }
    public MarinaPhotoKind Kind { get; set; }
    public required string StorageKey { get; set; }
    public string? UrlFull { get; set; }
    public string? UrlMedium { get; set; }
    public string? UrlThumbnail { get; set; }
    public int SortOrder { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? Caption { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public Guid UploadedByUserId { get; set; }
    public DateTimeOffset UploadedAt { get; init; } = DateTimeOffset.UtcNow;

    public Marina Marina { get; set; } = null!;
}
