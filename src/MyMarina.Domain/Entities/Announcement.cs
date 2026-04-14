using MyMarina.Domain.Common;

namespace MyMarina.Domain.Entities;

/// <summary>
/// A news/update post from a marina to its customers.
/// </summary>
public class Announcement : TenantEntity
{
    public Guid MarinaId { get; init; }
    public required string Title { get; set; }

    /// <summary>Markdown or rich text body.</summary>
    public required string Body { get; set; }

    /// <summary>Null = draft; set when published.</summary>
    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>Optional — hide the announcement after this date.</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    public bool IsPinned { get; set; }
    public Guid CreatedByUserId { get; init; }

    public Marina Marina { get; init; } = null!;
}
