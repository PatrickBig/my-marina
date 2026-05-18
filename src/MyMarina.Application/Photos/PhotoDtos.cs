namespace MyMarina.Application.Photos;

public sealed record MarinaPhotoDto(
    Guid Id,
    string Kind,
    string? UrlFull,
    string? UrlMedium,
    string? UrlThumbnail,
    int SortOrder,
    int? Width,
    int? Height,
    string? Caption,
    decimal? Latitude,
    decimal? Longitude,
    DateTimeOffset UploadedAt
);

public sealed record GetMarinaPhotosQuery(Guid MarinaId);
