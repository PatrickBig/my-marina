using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Announcements;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Announcements;

public class GetAnnouncementQueryHandler(AppDbContext db)
    : IQueryHandler<GetAnnouncementQuery, AnnouncementDto?>
{
    public async Task<AnnouncementDto?> HandleAsync(GetAnnouncementQuery query, CancellationToken ct = default)
    {
        return await db.Announcements
            .Where(a => a.Id == query.AnnouncementId)
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
            .FirstOrDefaultAsync(ct);
    }
}
