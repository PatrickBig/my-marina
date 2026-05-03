using MyMarina.Application.Sublet;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Sublet;

internal static class SubletHelper
{
    internal static OwnerAbsenceDto ToDto(OwnerAbsence a, string slipName) => new(
        Id:                a.Id,
        SlipAssignmentId:  a.SlipAssignmentId,
        SlipId:            a.SlipId,
        SlipName:          slipName,
        StartsOn:          a.StartsOn,
        EndsOn:            a.EndsOn,
        Notes:             a.Notes,
        CreatedAt:         a.CreatedAt);
}
