using MyMarina.Domain.Enums;

namespace MyMarina.Application.Photos;

public sealed record CreateUploadTicketCommand(
    Guid MarinaId,
    Guid RequestingUserId,
    MarinaPhotoKind Kind,
    string ContentType,
    long FileSizeBytes,
    int? ImageWidth,
    int? ImageHeight
);

public sealed record ConfirmPhotoUploadCommand(
    Guid MarinaId,
    Guid RequestingUserId,
    string Key,
    MarinaPhotoKind Kind,
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
