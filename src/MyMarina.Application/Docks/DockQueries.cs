using MyMarina.Application.Abstractions;

namespace MyMarina.Application.Docks;

public sealed record GetDocksQuery(Guid MarinaId);

public sealed record DockDto(
    Guid Id,
    Guid MarinaId,
    string Name,
    string? Description,
    int SortOrder,
    DateTimeOffset CreatedAt);

public interface IGetDocksQueryHandler : IQueryHandler<GetDocksQuery, IReadOnlyList<DockDto>>;
