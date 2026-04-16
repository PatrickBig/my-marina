using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Announcements;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Announcements;

public class DeleteAnnouncementCommandHandler(AppDbContext db) : ICommandHandler<DeleteAnnouncementCommand>
{
    public async Task HandleAsync(DeleteAnnouncementCommand command, CancellationToken ct = default)
    {
        var announcement = await db.Announcements
            .FirstOrDefaultAsync(a => a.Id == command.AnnouncementId, ct)
            ?? throw new KeyNotFoundException($"Announcement {command.AnnouncementId} not found.");

        db.Announcements.Remove(announcement);
        await db.SaveChangesAsync(ct);
    }
}
