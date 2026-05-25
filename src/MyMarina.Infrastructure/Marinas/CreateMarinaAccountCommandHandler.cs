using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class CreateMarinaAccountCommandHandler(
    UserManager<ApplicationUser> userManager,
    IJwtTokenService jwtTokenService,
    AppDbContext db,
    IConfiguration configuration)
    : ICommandHandler<CreateMarinaAccountCommand, MarinaSignupResponse>
{
    private static readonly HashSet<MarinaType> PrivateHostTypes =
        [MarinaType.PrivateDock, MarinaType.Dockominium];

    public async Task<MarinaSignupResponse> HandleAsync(CreateMarinaAccountCommand command, CancellationToken ct = default)
    {
        var isPrivateHost = PrivateHostTypes.Contains(command.MarinaType);

        if (isPrivateHost && (command.SlipName is null || command.MaxLength is null || command.MaxBeam is null || command.MaxDraft is null))
            throw new InvalidOperationException("SlipName, MaxLength, MaxBeam, and MaxDraft are required for PrivateDock and Dockominium accounts.");

        var user = await userManager.FindByIdAsync(command.UserId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        var tenantName = string.IsNullOrWhiteSpace(command.TenantName) ? command.MarinaName : command.TenantName;
        var tenantSlug = GenerateSlug(tenantName);
        var marinaSlug = GenerateSlug(command.MarinaName);

        // Ensure slug uniqueness by appending a short suffix when needed
        tenantSlug = await UniqueSlug(tenantSlug, slug => db.Tenants.Any(t => t.Slug == slug), ct);
        marinaSlug = await UniqueSlug(marinaSlug, slug => db.Marinas.Any(m => m.Slug == slug), ct);

        var tenant = new Tenant
        {
            Name = tenantName,
            Slug = tenantSlug,
        };

        var marina = new Marina
        {
            TenantId = tenant.Id,
            Name = command.MarinaName,
            Slug = marinaSlug,
            MarinaType = command.MarinaType,
            IsSetupComplete = isPrivateHost,
        };

        var membership = new Membership
        {
            UserId = command.UserId,
            Scope = MembershipScope.Marina,
            TenantId = tenant.Id,
            MarinaId = marina.Id,
            Role = MembershipRole.Owner,
            AcceptedAt = DateTimeOffset.UtcNow,
        };

        db.Tenants.Add(tenant);
        db.Marinas.Add(marina);
        db.Memberships.Add(membership);

        if (isPrivateHost)
        {
            var slip = new Slip
            {
                MarinaId         = marina.Id,
                Name             = command.SlipName!,
                SlipType         = command.SlipType,
                MaxLength        = command.MaxLength!.Value,
                MaxBeam          = command.MaxBeam!.Value,
                MaxDraft         = command.MaxDraft!.Value,
                Notes            = command.SlipNotes,
                HostMarinaId     = command.HostMarinaId,
                HostMarinaPolicy = command.HostMarinaPolicy,
            };
            db.Slips.Add(slip);
        }

        await db.SaveChangesAsync(ct);

        // Issue a fresh JWT that includes the new owner membership
        var tokenInfo = await AuthHelpers.BuildTokenInfoAsync(userManager, user, db, ct);
        var accessToken = jwtTokenService.GenerateAccessToken(tokenInfo);
        var (rawRefresh, hashedRefresh, expiresAt) = AuthHelpers.GenerateRefreshToken(configuration);
        db.RefreshTokens.Add(AuthHelpers.CreateRefreshTokenEntity(user.Id, hashedRefresh, expiresAt, command.IpAddress));
        await db.SaveChangesAsync(ct);

        return new MarinaSignupResponse(
            Tenant: MarinaMappers.ToTenantDto(tenant),
            Marina: MarinaMappers.ToMarinaDto(marina),
            AccessToken: accessToken,
            RefreshToken: rawRefresh,
            ExpiresAt: expiresAt
        );
    }

    private static string GenerateSlug(string name) =>
        System.Text.RegularExpressions.Regex.Replace(name.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-")
              .Trim('-');

    private static async Task<string> UniqueSlug(string base_, Func<string, bool> exists, CancellationToken ct)
    {
        if (!exists(base_)) return base_;
        for (var i = 2; i <= 99; i++)
        {
            var candidate = $"{base_}-{i}";
            if (!exists(candidate)) return candidate;
        }
        return $"{base_}-{Guid.CreateVersion7().ToString("N")[..8]}";
    }
}
