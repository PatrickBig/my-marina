using MyMarina.Domain.Enums;

namespace MyMarina.Application.Photos;

public sealed record UploadMarinaPhotoCommand(
    Guid MarinaId,
    Guid RequestingUserId,
    MarinaPhotoKind Kind,
    Stream Content,
    string ContentType,
    long FileSizeBytes,
    int Width,
    int Height,
    string? Caption,
    decimal? Latitude,
    decimal? Longitude
);

public sealed record ReorderPhotoCommand(
    Guid MarinaId,
    Guid RequestingUserId,
    Guid PhotoId,
    ReorderDirection Direction
);

public sealed record DeletePhotoCommand(
    Guid MarinaId,
    Guid RequestingUserId,
    Guid PhotoId
);

public enum ReorderDirection { Up, Down }
