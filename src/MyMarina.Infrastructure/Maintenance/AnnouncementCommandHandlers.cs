using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Maintenance;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Maintenance;

public class CreateAnnouncementCommandHandler(AppDbContext db, IUserContext user)
    : ICommandHandler<CreateAnnouncementCommand, AnnouncementDto>
{
    public async Task<AnnouncementDto> HandleAsync(
        CreateAnnouncementCommand cmd, CancellationToken ct = default)
    {
        var announcement = new Announcement
        {
            MarinaId        = cmd.MarinaId,
            Title           = cmd.Title,
            Body            = cmd.Body,
            Audience        = Enum.Parse<AnnouncementAudience>(cmd.Audience),
            IsPinned        = cmd.IsPinned,
            ExpiresAt       = cmd.ExpiresAt,
            CreatedByUserId = user.UserId!.Value,
        };

        db.Announcements.Add(announcement);
        await db.SaveChangesAsync(ct);
        return MaintenanceHelper.ToDto(announcement);
    }
}

public class UpdateAnnouncementCommandHandler(AppDbContext db)
    : ICommandHandler<UpdateAnnouncementCommand, AnnouncementDto>
{
    public async Task<AnnouncementDto> HandleAsync(
        UpdateAnnouncementCommand cmd, CancellationToken ct = default)
    {
        var announcement = await db.Announcements
            .FirstOrDefaultAsync(a => a.MarinaId == cmd.MarinaId && a.Id == cmd.AnnouncementId, ct)
            ?? throw new KeyNotFoundException("Announcement not found.");

        announcement.Title     = cmd.Title;
        announcement.Body      = cmd.Body;
        announcement.Audience  = Enum.Parse<AnnouncementAudience>(cmd.Audience);
        announcement.IsPinned  = cmd.IsPinned;
        announcement.ExpiresAt = cmd.ExpiresAt;

        await db.SaveChangesAsync(ct);
        return MaintenanceHelper.ToDto(announcement);
    }
}

public class PublishAnnouncementCommandHandler(AppDbContext db)
    : ICommandHandler<PublishAnnouncementCommand, AnnouncementDto>
{
    public async Task<AnnouncementDto> HandleAsync(
        PublishAnnouncementCommand cmd, CancellationToken ct = default)
    {
        var announcement = await db.Announcements
            .FirstOrDefaultAsync(a => a.MarinaId == cmd.MarinaId && a.Id == cmd.AnnouncementId, ct)
            ?? throw new KeyNotFoundException("Announcement not found.");

        announcement.PublishedAt ??= DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return MaintenanceHelper.ToDto(announcement);
    }
}

public class DeleteAnnouncementCommandHandler(AppDbContext db)
    : ICommandHandler<DeleteAnnouncementCommand>
{
    public async Task HandleAsync(DeleteAnnouncementCommand cmd, CancellationToken ct = default)
    {
        var announcement = await db.Announcements
            .FirstOrDefaultAsync(a => a.MarinaId == cmd.MarinaId && a.Id == cmd.AnnouncementId, ct)
            ?? throw new KeyNotFoundException("Announcement not found.");

        db.Announcements.Remove(announcement);
        await db.SaveChangesAsync(ct);
    }
}
