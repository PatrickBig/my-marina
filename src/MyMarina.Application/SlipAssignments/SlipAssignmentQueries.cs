using MyMarina.Application.Abstractions;
using MyMarina.Domain.Enums;

namespace MyMarina.Application.SlipAssignments;

/// <param name="SlipId">Filter to a specific slip. Null = all slips.</param>
/// <param name="CustomerAccountId">Filter to a specific customer. Null = all customers.</param>
/// <param name="ActiveOnly">When true, returns only assignments with no end date or end date in the future.</param>
public sealed record GetSlipAssignmentsQuery(
    Guid? SlipId,
    Guid? CustomerAccountId,
    bool ActiveOnly);

public sealed record SlipAssignmentDto(
    Guid Id,
    Guid SlipId,
    string SlipName,
    Guid CustomerAccountId,
    string CustomerDisplayName,
    Guid BoatId,
    string BoatName,
    AssignmentType AssignmentType,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal? RateOverride,
    string? Notes,
    DateTimeOffset CreatedAt);

public interface IGetSlipAssignmentsQueryHandler : IQueryHandler<GetSlipAssignmentsQuery, IReadOnlyList<SlipAssignmentDto>>;
