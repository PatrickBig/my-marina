using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Announcements;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Announcements;

public class UnpublishAnnouncementCommandHandler(AppDbContext db) : ICommandHandler<UnpublishAnnouncementCommand>
{
    public async Task HandleAsync(UnpublishAnnouncementCommand command, CancellationToken ct = default)
    {
        var announcement = await db.Announcements
            .FirstOrDefaultAsync(a => a.Id == command.AnnouncementId, ct)
            ?? throw new KeyNotFoundException($"Announcement {command.AnnouncementId} not found.");

        announcement.PublishedAt = null;
        await db.SaveChangesAsync(ct);
    }
}
