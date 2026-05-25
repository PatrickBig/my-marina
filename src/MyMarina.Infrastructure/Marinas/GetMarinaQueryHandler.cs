using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class GetMarinaQueryHandler(AppDbContext db)
    : IQueryHandler<GetMarinaQuery, MarinaDto>
{
    public async Task<MarinaDto> HandleAsync(GetMarinaQuery query, CancellationToken ct = default)
    {
        // IgnoreQueryFilters: owners must be able to fetch their draft marina during onboarding.
        // The controller already enforces HasMarinaAccess() before calling this handler.
        var marina = await db.Marinas
            .IgnoreQueryFilters()
            .Include(m => m.Photos)
            .FirstOrDefaultAsync(m => m.Id == query.MarinaId, ct)
            ?? throw new KeyNotFoundException("Marina not found.");

        var logoUrl = marina.Photos
            .Where(p => p.Kind == MarinaPhotoKind.Logo)
            .Select(p => p.UrlThumbnail)
            .FirstOrDefault();
        var bannerUrl = marina.Photos
            .Where(p => p.Kind == MarinaPhotoKind.Banner)
            .Select(p => p.UrlMedium)
            .FirstOrDefault();

        return MarinaMappers.ToMarinaDto(marina, logoUrl, bannerUrl);
    }
}
