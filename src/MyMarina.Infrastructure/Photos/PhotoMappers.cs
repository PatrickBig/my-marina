using MyMarina.Application.Photos;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Photos;

internal static class PhotoMappers
{
    internal static MarinaPhotoDto ToDto(MarinaPhoto p) => new(
        Id: p.Id,
        Kind: p.Kind.ToString(),
        UrlFull: p.UrlFull,
        UrlMedium: p.UrlMedium,
        UrlThumbnail: p.UrlThumbnail,
        SortOrder: p.SortOrder,
        Width: p.Width,
        Height: p.Height,
        Caption: p.Caption,
        Latitude: p.Latitude,
        Longitude: p.Longitude,
        UploadedAt: p.UploadedAt
    );
}
