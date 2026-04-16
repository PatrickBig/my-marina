using MyMarina.Application.Abstractions;
using MyMarina.Application.Announcements;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Announcements;

public class CreateAnnouncementCommandHandler(
    AppDbContext db,
    ITenantContext tenantContext) : ICommandHandler<CreateAnnouncementCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateAnnouncementCommand command, CancellationToken ct = default)
    {
        var announcement = new Announcement
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantContext.TenantId,
            MarinaId = command.MarinaId,
            Title = command.Title,
            Body = command.Body,
            IsPinned = command.IsPinned,
            ExpiresAt = command.ExpiresAt,
            CreatedByUserId = command.CreatedByUserId,
            PublishedAt = command.Publish ? DateTimeOffset.UtcNow : null,
        };

        db.Announcements.Add(announcement);
        await db.SaveChangesAsync(ct);
        return announcement.Id;
    }
}
