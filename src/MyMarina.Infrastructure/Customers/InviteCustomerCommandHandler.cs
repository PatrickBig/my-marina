using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Customers;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Customers;

public class InviteCustomerCommandHandler(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    ITenantContext tenantContext) : ICommandHandler<InviteCustomerCommand, InviteCustomerResult>
{
    public async Task<InviteCustomerResult> HandleAsync(InviteCustomerCommand command, CancellationToken ct = default)
    {
        var account = await db.CustomerAccounts
            .FirstOrDefaultAsync(a => a.Id == command.CustomerAccountId, ct)
            ?? throw new KeyNotFoundException($"CustomerAccount {command.CustomerAccountId} not found.");

        var existing = await userManager.FindByEmailAsync(command.Email);
        if (existing is not null)
            throw new InvalidOperationException($"A user with email '{command.Email}' already exists.");

        var temporaryPassword = $"Temp_{Guid.NewGuid():N}!C1";

        var user = new ApplicationUser
        {
            UserName = command.Email,
            Email = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName,
            Role = UserRole.Customer,
            TenantId = tenantContext.TenantId,
        };

        var result = await userManager.CreateAsync(user, temporaryPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create customer user: {errors}");
        }

        var member = new CustomerAccountMember
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantContext.TenantId,
            CustomerAccountId = account.Id,
            UserId = user.Id,
            Role = CustomerAccountMemberRole.Owner,
        };
        db.CustomerAccountMembers.Add(member);
        await db.SaveChangesAsync(ct);

        return new InviteCustomerResult(user.Id, temporaryPassword);
    }
}
