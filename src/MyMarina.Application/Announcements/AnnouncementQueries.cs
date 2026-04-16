using MyMarina.Application.Abstractions;

namespace MyMarina.Application.Announcements;

/// <param name="MarinaId">Filter to a specific marina. Required for marina-scoped staff.</param>
/// <param name="IncludeDrafts">When true, include unpublished drafts. Default false.</param>
/// <param name="IncludeExpired">When true, include expired announcements. Default false.</param>
public sealed record GetAnnouncementsQuery(
    Guid MarinaId,
    bool IncludeDrafts = true,
    bool IncludeExpired = true);

public sealed record GetAnnouncementQuery(Guid AnnouncementId);

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record AnnouncementDto(
    Guid Id,
    Guid MarinaId,
    string Title,
    string Body,
    bool IsPinned,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ExpiresAt,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt);

// ── Handler interfaces ────────────────────────────────────────────────────────

public interface IGetAnnouncementsQueryHandler : IQueryHandler<GetAnnouncementsQuery, IReadOnlyList<AnnouncementDto>>;
public interface IGetAnnouncementQueryHandler : IQueryHandler<GetAnnouncementQuery, AnnouncementDto?>;
