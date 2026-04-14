using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ApplyTenantFilters(builder);
    }

    private void ApplyTenantFilters(ModelBuilder builder)
    {
        // Platform operators bypass all tenant filters
        if (tenantContext.IsPlatformOperator)
            return;

        var tenantId = tenantContext.TenantId;

        builder.Entity<Marina>().HasQueryFilter(e => e.TenantId == tenantId);
        builder.Entity<Dock>().HasQueryFilter(e => e.TenantId == tenantId);
        builder.Entity<Slip>().HasQueryFilter(e => e.TenantId == tenantId);
        builder.Entity<SlipAssignment>().HasQueryFilter(e => e.TenantId == tenantId);
        builder.Entity<CustomerAccount>().HasQueryFilter(e => e.TenantId == tenantId);
        builder.Entity<CustomerAccountMember>().HasQueryFilter(e => e.TenantId == tenantId);
        builder.Entity<Boat>().HasQueryFilter(e => e.TenantId == tenantId);
        builder.Entity<Invoice>().HasQueryFilter(e => e.TenantId == tenantId);
        builder.Entity<InvoiceLineItem>().HasQueryFilter(e => e.TenantId == tenantId);
        builder.Entity<Payment>().HasQueryFilter(e => e.TenantId == tenantId);
        builder.Entity<MaintenanceRequest>().HasQueryFilter(e => e.TenantId == tenantId);
        builder.Entity<WorkOrder>().HasQueryFilter(e => e.TenantId == tenantId);
        builder.Entity<Announcement>().HasQueryFilter(e => e.TenantId == tenantId);
    }
}
