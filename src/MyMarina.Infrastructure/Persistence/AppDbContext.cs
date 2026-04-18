using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using RoleNames = MyMarina.Domain.Common.Roles;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure.Identity;

namespace MyMarina.Infrastructure.Persistence;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ITenantContext tenantContext)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Marina> Marinas => Set<Marina>();
    public DbSet<Dock> Docks => Set<Dock>();
    public DbSet<Slip> Slips => Set<Slip>();
    public DbSet<SlipAssignment> SlipAssignments => Set<SlipAssignment>();
    public DbSet<CustomerAccount> CustomerAccounts => Set<CustomerAccount>();
    public DbSet<CustomerAccountMember> CustomerAccountMembers => Set<CustomerAccountMember>();
    public DbSet<Boat> Boats => Set<Boat>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<UserContext> UserContexts => Set<UserContext>();
    public DbSet<Role> AuthorizationRoles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<OperatingExpense> OperatingExpenses => Set<OperatingExpense>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("mymarina");
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ApplyTenantFilters(builder);
        SeedRoles(builder);
    }

    private void ApplyTenantFilters(ModelBuilder builder)
    {
        // Platform operators bypass all tenant filters.
        // Reference tenantContext directly (not a captured local Guid) so EF re-evaluates
        // the property on every query using the scoped ITenantContext for the current request.
        builder.Entity<Marina>().HasQueryFilter(e => tenantContext.IsPlatformOperator || e.TenantId == tenantContext.TenantId);
        builder.Entity<Dock>().HasQueryFilter(e => tenantContext.IsPlatformOperator || e.TenantId == tenantContext.TenantId);
        builder.Entity<Slip>().HasQueryFilter(e => tenantContext.IsPlatformOperator || e.TenantId == tenantContext.TenantId);
        builder.Entity<SlipAssignment>().HasQueryFilter(e => tenantContext.IsPlatformOperator || e.TenantId == tenantContext.TenantId);
        builder.Entity<CustomerAccount>().HasQueryFilter(e => tenantContext.IsPlatformOperator || e.TenantId == tenantContext.TenantId);
        builder.Entity<CustomerAccountMember>().HasQueryFilter(e => tenantContext.IsPlatformOperator || e.TenantId == tenantContext.TenantId);
        builder.Entity<Boat>().HasQueryFilter(e => tenantContext.IsPlatformOperator || e.TenantId == tenantContext.TenantId);
        builder.Entity<Invoice>().HasQueryFilter(e => tenantContext.IsPlatformOperator || e.TenantId == tenantContext.TenantId);
        builder.Entity<InvoiceLineItem>().HasQueryFilter(e => tenantContext.IsPlatformOperator || e.TenantId == tenantContext.TenantId);
        builder.Entity<Payment>().HasQueryFilter(e => tenantContext.IsPlatformOperator || e.TenantId == tenantContext.TenantId);
        builder.Entity<MaintenanceRequest>().HasQueryFilter(e => tenantContext.IsPlatformOperator || e.TenantId == tenantContext.TenantId);
        builder.Entity<WorkOrder>().HasQueryFilter(e => tenantContext.IsPlatformOperator || e.TenantId == tenantContext.TenantId);
        builder.Entity<Announcement>().HasQueryFilter(e => tenantContext.IsPlatformOperator || e.TenantId == tenantContext.TenantId);
        builder.Entity<OperatingExpense>().HasQueryFilter(e => tenantContext.IsPlatformOperator || e.TenantId == tenantContext.TenantId);
    }

    private void SeedRoles(ModelBuilder builder)
    {
        var roles = new[]
        {
            new Role { Id = Guid.Parse("00000001-0000-0000-0000-000000000001"), Name = RoleNames.PlatformAdmin, Description = "System administrator with cross-tenant access" },
            new Role { Id = Guid.Parse("00000002-0000-0000-0000-000000000001"), Name = RoleNames.TenantOwner,   Description = "Owns a tenant and sees all marinas within it" },
            new Role { Id = Guid.Parse("00000003-0000-0000-0000-000000000001"), Name = RoleNames.MarinaManager, Description = "Manages a specific marina" },
            new Role { Id = Guid.Parse("00000004-0000-0000-0000-000000000001"), Name = RoleNames.MarinaStaff,   Description = "Staff member at a specific marina" },
            new Role { Id = Guid.Parse("00000005-0000-0000-0000-000000000001"), Name = RoleNames.Customer,      Description = "Boat owner with portal access" },
        };

        builder.Entity<Role>().HasData(roles);
    }
}
