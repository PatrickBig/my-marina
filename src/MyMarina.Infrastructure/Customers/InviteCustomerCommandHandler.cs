using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Customers;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Common;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Email;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Customers;

public class InviteCustomerCommandHandler(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    ITenantContext tenantContext,
    IMarinaContext marinaContext,
    IEmailService emailService,
    IOptions<EmailOptions> emailOptions,
    ILogger<InviteCustomerCommandHandler> logger) : ICommandHandler<InviteCustomerCommand, InviteCustomerResult>
{
    public async Task<InviteCustomerResult> HandleAsync(InviteCustomerCommand command, CancellationToken ct = default)
    {
        var account = await db.CustomerAccounts
            .FirstOrDefaultAsync(a => a.Id == command.CustomerAccountId && a.TenantId == tenantContext.TenantId, ct)
            ?? throw new KeyNotFoundException($"CustomerAccount {command.CustomerAccountId} not found.");

        // Check if customer already has a user (1:1 constraint for now)
        var existingMember = await db.CustomerAccountMembers
            .Where(m => m.CustomerAccountId == command.CustomerAccountId)
            .FirstOrDefaultAsync(ct);
        if (existingMember is not null)
            throw new InvalidOperationException($"This customer account already has a login associated.");

        var existingUser = await userManager.FindByEmailAsync(account.BillingEmail);
        if (existingUser is not null)
            throw new InvalidOperationException($"A user with email '{account.BillingEmail}' already exists.");

        var temporaryPassword = $"Temp_{Guid.NewGuid():N}!C1";

        var user = new ApplicationUser
        {
            UserName = account.BillingEmail,
            Email = account.BillingEmail,
            FirstName = "Customer",
            LastName = account.DisplayName,
        };

        var result = await userManager.CreateAsync(user, temporaryPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create customer user: {errors}");
        }

        var customerRole = await db.AuthorizationRoles.FirstAsync(r => r.Name == Roles.Customer, ct);
        var userContext = new UserContext
        {
            UserId = user.Id,
            RoleId = customerRole.Id,
            TenantId = tenantContext.TenantId,
            CustomerAccountId = account.Id,
        };
        db.UserContexts.Add(userContext);

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

        // Send invite email — non-fatal if delivery fails
        try
        {
            var marinaName = marinaContext.MarinaId.HasValue
                ? (await db.Marinas.Where(m => m.Id == marinaContext.MarinaId.Value).Select(m => m.Name).FirstOrDefaultAsync(ct) ?? "Your Marina")
                : "Your Marina";

            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = Uri.EscapeDataString(token);
            var confirmationLink = $"{emailOptions.Value.AppBaseUrl}/confirm-email?userId={user.Id}&token={encodedToken}";
            await emailService.SendCustomerInviteAsync(
                account.BillingEmail,
                account.DisplayName,
                marinaName,
                temporaryPassword,
                confirmationLink,
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send invite email to customer {CustomerAccountId}", command.CustomerAccountId);
        }

        return new InviteCustomerResult(user.Id, temporaryPassword);
    }
}
