using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Portal;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Portal;

public class GetPortalAnnouncementsQueryHandler(
    AppDbContext db) : IQueryHandler<GetPortalAnnouncementsQuery, IReadOnlyList<PortalAnnouncementDto>>
{
    public async Task<IReadOnlyList<PortalAnnouncementDto>> HandleAsync(
        GetPortalAnnouncementsQuery query, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        return await db.Announcements
            .Where(a => a.PublishedAt != null
                     && a.PublishedAt <= now
                     && (a.ExpiresAt == null || a.ExpiresAt > now))
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.PublishedAt)
            .Select(a => new PortalAnnouncementDto(
                a.Id,
                a.Title,
                a.Body,
                a.IsPinned,
                a.PublishedAt!.Value,
                a.ExpiresAt,
                a.Marina.Name))
            .ToListAsync(ct);
    }
}
