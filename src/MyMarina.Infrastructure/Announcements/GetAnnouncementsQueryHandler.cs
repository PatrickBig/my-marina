using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Announcements;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Announcements;

public class GetAnnouncementsQueryHandler(AppDbContext db)
    : IQueryHandler<GetAnnouncementsQuery, IReadOnlyList<AnnouncementDto>>
{
    public async Task<IReadOnlyList<AnnouncementDto>> HandleAsync(GetAnnouncementsQuery query, CancellationToken ct = default)
    {
        var q = db.Announcements
            .Where(a => a.MarinaId == query.MarinaId)
            .AsQueryable();

        if (!query.IncludeDrafts)
            q = q.Where(a => a.PublishedAt != null);

        if (!query.IncludeExpired)
            q = q.Where(a => a.ExpiresAt == null || a.ExpiresAt > DateTimeOffset.UtcNow);

        var items = await q
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new AnnouncementDto(
                a.Id,
                a.MarinaId,
                a.Title,
                a.Body,
                a.IsPinned,
                a.PublishedAt != null,
                a.PublishedAt,
                a.ExpiresAt,
                a.CreatedByUserId,
                a.CreatedAt))
            .ToListAsync(ct);

        return items;
    }
}
