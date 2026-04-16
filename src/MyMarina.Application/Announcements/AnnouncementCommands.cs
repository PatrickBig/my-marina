using MyMarina.Application.Abstractions;

namespace MyMarina.Application.Announcements;

public sealed record CreateAnnouncementCommand(
    Guid MarinaId,
    string Title,
    string Body,
    bool Publish,
    bool IsPinned,
    DateTimeOffset? ExpiresAt,
    Guid CreatedByUserId);

public sealed record UpdateAnnouncementCommand(
    Guid AnnouncementId,
    string Title,
    string Body,
    bool IsPinned,
    DateTimeOffset? ExpiresAt);

public sealed record PublishAnnouncementCommand(Guid AnnouncementId);

public sealed record UnpublishAnnouncementCommand(Guid AnnouncementId);

public sealed record DeleteAnnouncementCommand(Guid AnnouncementId);

public interface ICreateAnnouncementCommandHandler : ICommandHandler<CreateAnnouncementCommand, Guid>;
public interface IUpdateAnnouncementCommandHandler : ICommandHandler<UpdateAnnouncementCommand>;
public interface IPublishAnnouncementCommandHandler : ICommandHandler<PublishAnnouncementCommand>;
public interface IUnpublishAnnouncementCommandHandler : ICommandHandler<UnpublishAnnouncementCommand>;
public interface IDeleteAnnouncementCommandHandler : ICommandHandler<DeleteAnnouncementCommand>;
