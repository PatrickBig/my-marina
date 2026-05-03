using MyMarina.Domain.Enums;

namespace MyMarina.Domain.Entities;

public class Announcement
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid MarinaId { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public AnnouncementAudience Audience { get; set; } = AnnouncementAudience.Both;
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsPinned { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public Marina Marina { get; set; } = null!;
}
