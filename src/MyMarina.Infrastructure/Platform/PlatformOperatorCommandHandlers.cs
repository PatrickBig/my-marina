using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Platform;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Platform;

public class CreateTenantCommandHandler(AppDbContext db, IUserContext user)
    : ICommandHandler<CreateTenantCommand, CreateTenantResponse>
{
    public async Task<CreateTenantResponse> HandleAsync(CreateTenantCommand command, CancellationToken ct = default)
    {
        var tier = Enum.TryParse<SubscriptionTier>(command.SubscriptionTier, out var t) ? t : SubscriptionTier.Free;
        var tenant = new Tenant
        {
            Name = command.Name,
            Slug = command.Slug,
            BillingEmail = command.BillingEmail,
            SubscriptionTier = tier,
        };
        db.Tenants.Add(tenant);

        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = user.UserId,
            Action = "tenant.created",
            TargetType = "Tenant",
            TargetId = tenant.Id.ToString(),
            Details = $"Created tenant '{command.Name}' (slug: {command.Slug})",
        });

        await db.SaveChangesAsync(ct);
        return new CreateTenantResponse(tenant.Id, tenant.Name, tenant.Slug);
    }
}

public class SuspendTenantCommandHandler(AppDbContext db, IUserContext user)
    : ICommandHandler<SuspendTenantCommand>
{
    public async Task HandleAsync(SuspendTenantCommand command, CancellationToken ct = default)
    {
        var tenant = await db.Tenants.FindAsync([command.TenantId], ct)
            ?? throw new KeyNotFoundException("Tenant not found.");

        tenant.IsActive = false;
        tenant.SuspendedAt = DateTimeOffset.UtcNow;

        db.AuditLogs.Add(new AuditLog
        {
            TenantId = command.TenantId,
            ActorUserId = user.UserId,
            Action = "tenant.suspended",
            TargetType = "Tenant",
            TargetId = command.TenantId.ToString(),
            Details = command.Reason,
        });

        await db.SaveChangesAsync(ct);
    }
}

public class ReactivateTenantCommandHandler(AppDbContext db, IUserContext user)
    : ICommandHandler<ReactivateTenantCommand>
{
    public async Task HandleAsync(ReactivateTenantCommand command, CancellationToken ct = default)
    {
        var tenant = await db.Tenants.FindAsync([command.TenantId], ct)
            ?? throw new KeyNotFoundException("Tenant not found.");

        tenant.IsActive = true;
        tenant.SuspendedAt = null;

        db.AuditLogs.Add(new AuditLog
        {
            TenantId = command.TenantId,
            ActorUserId = user.UserId,
            Action = "tenant.reactivated",
            TargetType = "Tenant",
            TargetId = command.TenantId.ToString(),
        });

        await db.SaveChangesAsync(ct);
    }
}

public class ForceSignOutCommandHandler(AppDbContext db, IUserContext user)
    : ICommandHandler<ForceSignOutCommand>
{
    public async Task HandleAsync(ForceSignOutCommand command, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        await db.RefreshTokens
            .Where(t => t.UserId == command.TargetUserId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);

        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = user.UserId,
            Action = "user.force_signout",
            TargetType = "User",
            TargetId = command.TargetUserId.ToString(),
        });

        await db.SaveChangesAsync(ct);
    }
}

public class DeactivateUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    IUserContext user)
    : ICommandHandler<DeactivateUserCommand>
{
    public async Task HandleAsync(DeactivateUserCommand command, CancellationToken ct = default)
    {
        var appUser = await userManager.FindByIdAsync(command.TargetUserId.ToString())
            ?? throw new KeyNotFoundException("User not found.");

        appUser.IsActive = false;
        await userManager.UpdateAsync(appUser);

        var now = DateTimeOffset.UtcNow;
        await db.RefreshTokens
            .Where(t => t.UserId == command.TargetUserId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);

        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = user.UserId,
            Action = "user.deactivated",
            TargetType = "User",
            TargetId = command.TargetUserId.ToString(),
            Details = command.Reason,
        });

        await db.SaveChangesAsync(ct);
    }
}

public class ActivateUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    IUserContext user)
    : ICommandHandler<ActivateUserCommand>
{
    public async Task HandleAsync(ActivateUserCommand command, CancellationToken ct = default)
    {
        var appUser = await userManager.FindByIdAsync(command.TargetUserId.ToString())
            ?? throw new KeyNotFoundException("User not found.");

        appUser.IsActive = true;
        await userManager.UpdateAsync(appUser);

        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = user.UserId,
            Action = "user.activated",
            TargetType = "User",
            TargetId = command.TargetUserId.ToString(),
        });

        await db.SaveChangesAsync(ct);
    }
}

public class RemoveListingCommandHandler(AppDbContext db, IUserContext user)
    : ICommandHandler<RemoveListingCommand>
{
    public async Task HandleAsync(RemoveListingCommand command, CancellationToken ct = default)
    {
        var window = await db.AvailabilityWindows.FindAsync([command.ListingId], ct)
            ?? throw new KeyNotFoundException("Listing not found.");

        window.Status = AvailabilityWindowStatus.Closed;

        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = user.UserId,
            Action = "listing.removed",
            TargetType = "AvailabilityWindow",
            TargetId = command.ListingId.ToString(),
            Details = command.Reason,
        });

        await db.SaveChangesAsync(ct);
    }
}
