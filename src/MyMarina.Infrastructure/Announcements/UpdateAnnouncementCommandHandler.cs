using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Announcements;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Announcements;

public class UpdateAnnouncementCommandHandler(AppDbContext db) : ICommandHandler<UpdateAnnouncementCommand>
{
    public async Task HandleAsync(UpdateAnnouncementCommand command, CancellationToken ct = default)
    {
        var announcement = await db.Announcements
            .FirstOrDefaultAsync(a => a.Id == command.AnnouncementId, ct)
            ?? throw new KeyNotFoundException($"Announcement {command.AnnouncementId} not found.");

        announcement.Title = command.Title;
        announcement.Body = command.Body;
        announcement.IsPinned = command.IsPinned;
        announcement.ExpiresAt = command.ExpiresAt;

        await db.SaveChangesAsync(ct);
    }
}
