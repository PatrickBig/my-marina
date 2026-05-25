using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Photos;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Photos;

public class GetMarinaPhotosQueryHandler(AppDbContext db)
    : IQueryHandler<GetMarinaPhotosQuery, IReadOnlyList<MarinaPhotoDto>>
{
    public async Task<IReadOnlyList<MarinaPhotoDto>> HandleAsync(GetMarinaPhotosQuery query, CancellationToken ct = default)
    {
        var photos = await db.MarinaPhotos
            .AsNoTracking()
            .Where(p => p.MarinaId == query.MarinaId)
            .OrderBy(p => p.Kind)
            .ThenBy(p => p.SortOrder)
            .Select(p => PhotoMappers.ToDto(p))
            .ToListAsync(ct);

        return photos;
    }
}
