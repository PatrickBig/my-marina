using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Announcements;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Announcements;

public class PublishAnnouncementCommandHandler(AppDbContext db) : ICommandHandler<PublishAnnouncementCommand>
{
    public async Task HandleAsync(PublishAnnouncementCommand command, CancellationToken ct = default)
    {
        var announcement = await db.Announcements
            .FirstOrDefaultAsync(a => a.Id == command.AnnouncementId, ct)
            ?? throw new KeyNotFoundException($"Announcement {command.AnnouncementId} not found.");

        if (announcement.PublishedAt is not null)
            throw new InvalidOperationException("Announcement is already published.");

        announcement.PublishedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
