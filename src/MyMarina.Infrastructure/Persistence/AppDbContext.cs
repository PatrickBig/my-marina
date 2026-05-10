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
    public DbSet<SlipAssignment> SlipAssignments => Set<SlipAssignment>();
    public DbSet<AvailabilityWindow> AvailabilityWindows => Set<AvailabilityWindow>();
    public DbSet<OwnerAbsence> OwnerAbsences => Set<OwnerAbsence>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<SlipLeaseInquiry> SlipLeaseInquiries => Set<SlipLeaseInquiry>();
    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("mymarina");
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Draft marinas are invisible to all queries by default.
        // Handlers that need draft access (owner wizard, GetMyMarinas) use IgnoreQueryFilters().
        builder.Entity<Domain.Entities.Marina>()
               .HasQueryFilter(m => m.IsSetupComplete);
    }
}
