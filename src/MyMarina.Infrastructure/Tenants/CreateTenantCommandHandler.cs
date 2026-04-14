using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Tenants;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Tenants;

public class CreateTenantCommandHandler(
    AppDbContext db,
    UserManager<ApplicationUser> userManager) : ICommandHandler<CreateTenantCommand, CreateTenantResult>
{
    public async Task<CreateTenantResult> HandleAsync(CreateTenantCommand command, CancellationToken ct = default)
    {
        var slugTaken = await db.Tenants.AnyAsync(t => t.Slug == command.Slug, ct);
        if (slugTaken)
            throw new InvalidOperationException($"Slug '{command.Slug}' is already in use.");

        var tenant = new Tenant
        {
            Name = command.Name,
            Slug = command.Slug,
            SubscriptionTier = command.SubscriptionTier,
        };

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(ct);

        var owner = new ApplicationUser
        {
            UserName = command.OwnerEmail,
            Email = command.OwnerEmail,
            FirstName = command.OwnerFirstName,
            LastName = command.OwnerLastName,
            Role = UserRole.MarinaOwner,
            TenantId = tenant.Id,
        };

        var result = await userManager.CreateAsync(owner, command.OwnerPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create marina owner: {errors}");
        }

        return new CreateTenantResult(tenant.Id, owner.Id);
    }
}
