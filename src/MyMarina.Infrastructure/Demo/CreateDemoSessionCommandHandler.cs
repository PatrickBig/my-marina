using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Demo;
using MyMarina.Domain.Common;
using MyMarina.Infrastructure.Identity;

namespace MyMarina.Infrastructure.Demo;

public class CreateDemoSessionCommandHandler(
    ICommandHandler<ProvisionDemoTenantCommand, ProvisionDemoTenantResult> provisionHandler,
    UserManager<ApplicationUser> userManager,
    IJwtTokenService jwtTokenService,
    IOptions<DemoOptions> options) : ICommandHandler<CreateDemoSessionCommand, DemoSessionResult>
{
    public async Task<DemoSessionResult> HandleAsync(
        CreateDemoSessionCommand command, CancellationToken ct = default)
    {
        var provision = await provisionHandler.HandleAsync(
            new ProvisionDemoTenantCommand(command.Role, command.Tier), ct);

        var ttl = options.Value.TtlMinutes;
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(ttl);

        UserTokenInfo tokenInfo;
        if (command.Role == "customer")
        {
            var custUser = await userManager.FindByIdAsync(provision.CustomerUserId.ToString())
                ?? throw new InvalidOperationException("Demo customer user not found after provisioning.");

            tokenInfo = new UserTokenInfo(
                UserId: custUser.Id,
                Email: custUser.Email!,
                FirstName: custUser.FirstName,
                LastName: custUser.LastName,
                Role: Roles.Customer,
                TenantId: provision.TenantId,
                MarinaId: null,
                CustomerAccountId: provision.CustomerAccountId,
                CustomerAccountIds: [provision.CustomerAccountId],
                SubscriptionTier: command.Tier,
                IsDemo: true);
        }
        else
        {
            var opUser = await userManager.FindByIdAsync(provision.OperatorUserId.ToString())
                ?? throw new InvalidOperationException("Demo operator user not found after provisioning.");

            tokenInfo = new UserTokenInfo(
                UserId: opUser.Id,
                Email: opUser.Email!,
                FirstName: opUser.FirstName,
                LastName: opUser.LastName,
                Role: Roles.TenantOwner,
                TenantId: provision.TenantId,
                MarinaId: null,
                SubscriptionTier: command.Tier,
                IsDemo: true);
        }

        // Override token expiry to match demo TTL
        var token = jwtTokenService.GenerateToken(tokenInfo with { });

        return new DemoSessionResult(Token: token, ExpiresAt: expiresAt);
    }
}
