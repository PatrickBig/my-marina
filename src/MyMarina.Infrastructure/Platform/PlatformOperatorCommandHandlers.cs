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

public class ChangeUserEmailCommandHandler(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    IUserContext user)
    : ICommandHandler<ChangeUserEmailCommand>
{
    public async Task HandleAsync(ChangeUserEmailCommand command, CancellationToken ct = default)
    {
        var appUser = await userManager.FindByIdAsync(command.TargetUserId.ToString())
            ?? throw new KeyNotFoundException("User not found.");

        var oldEmail = appUser.Email;

        var existingUser = await userManager.FindByEmailAsync(command.NewEmail);
        if (existingUser != null && existingUser.Id != appUser.Id)
            throw new InvalidOperationException("Email already in use.");

        appUser.Email = command.NewEmail;
        appUser.UserName = command.NewEmail;
        appUser.EmailConfirmed = true;

        var result = await userManager.UpdateAsync(appUser);
        if (!result.Succeeded)
            throw new InvalidOperationException("Failed to update user email.");

        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = user.UserId,
            Action = "user.email_changed",
            TargetType = "User",
            TargetId = command.TargetUserId.ToString(),
            Details = $"Changed email from {oldEmail} to {command.NewEmail}",
        });

        await db.SaveChangesAsync(ct);
    }
}

public class ChangeUserNameCommandHandler(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    IUserContext user)
    : ICommandHandler<ChangeUserNameCommand>
{
    public async Task HandleAsync(ChangeUserNameCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.FirstName) && string.IsNullOrWhiteSpace(command.LastName))
            throw new InvalidOperationException("At least one of FirstName or LastName must be provided.");

        var appUser = await userManager.FindByIdAsync(command.TargetUserId.ToString())
            ?? throw new KeyNotFoundException("User not found.");

        if (!string.IsNullOrWhiteSpace(command.FirstName))
        {
            var oldFirstName = appUser.FirstName;
            appUser.FirstName = command.FirstName;

            db.AuditLogs.Add(new AuditLog
            {
                ActorUserId = user.UserId,
                Action = "user.first_name_changed",
                TargetType = "User",
                TargetId = command.TargetUserId.ToString(),
                Details = $"Changed first name from {oldFirstName} to {command.FirstName}",
            });
        }

        if (!string.IsNullOrWhiteSpace(command.LastName))
        {
            var oldLastName = appUser.LastName;
            appUser.LastName = command.LastName;

            db.AuditLogs.Add(new AuditLog
            {
                ActorUserId = user.UserId,
                Action = "user.last_name_changed",
                TargetType = "User",
                TargetId = command.TargetUserId.ToString(),
                Details = $"Changed last name from {oldLastName} to {command.LastName}",
            });
        }

        var result = await userManager.UpdateAsync(appUser);
        if (!result.Succeeded)
            throw new InvalidOperationException("Failed to update user name.");

        await db.SaveChangesAsync(ct);
    }
}

public class InitiatePasswordResetCommandHandler(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    IUserContext user,
    IEmailService emailService)
    : ICommandHandler<InitiatePasswordResetCommand>
{
    public async Task HandleAsync(InitiatePasswordResetCommand command, CancellationToken ct = default)
    {
        var appUser = await userManager.FindByIdAsync(command.TargetUserId.ToString())
            ?? throw new KeyNotFoundException("User not found.");

        var now = DateTimeOffset.UtcNow;
        await db.RefreshTokens
            .Where(t => t.UserId == command.TargetUserId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);

        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = user.UserId,
            Action = "user.password_reset_requested",
            TargetType = "User",
            TargetId = command.TargetUserId.ToString(),
            Details = "Password reset initiated by operator",
        });

        await db.SaveChangesAsync(ct);

        var token = await userManager.GeneratePasswordResetTokenAsync(appUser);
        await emailService.SendPasswordResetAsync(appUser.Email!, appUser.Id.ToString(), token, ct);
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
