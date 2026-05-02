using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure.Identity;

namespace MyMarina.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Vessel> Vessels => Set<Vessel>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Marina> Marinas => Set<Marina>();
    public DbSet<Dock> Docks => Set<Dock>();
    public DbSet<Slip> Slips => Set<Slip>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<BillingAccount> BillingAccounts => Set<BillingAccount>();
    public DbSet<BillingAccountMember> BillingAccountMembers => Set<BillingAccountMember>();
    public DbSet<MarinaVesselRecord> MarinaVesselRecords => Set<MarinaVesselRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("mymarina");
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
