using MyMarina.Domain.Common;
using MyMarina.Domain.Enums;

namespace MyMarina.Domain.Entities;

/// <summary>
/// Links a User to a CustomerAccount with a role.
/// Multiple members may belong to the same account.
/// </summary>
public class CustomerAccountMember : TenantEntity
{
    public Guid CustomerAccountId { get; init; }
    public Guid UserId { get; init; }
    public CustomerAccountMemberRole Role { get; set; }

    public CustomerAccount CustomerAccount { get; init; } = null!;
}
