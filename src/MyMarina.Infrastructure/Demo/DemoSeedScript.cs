// MAINTENANCE: This is a living document. When a new phase adds a new entity type or capability,
// add representative seed records here in the same PR. See CLAUDE.md for the standing rule.
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyMarina.Domain.Common;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Demo;

/// <summary>
/// Seeds a demo tenant with two fully-populated marinas covering every platform capability.
/// Called by ProvisionDemoTenantCommandHandler when creating a per-visitor demo tenant.
/// </summary>
public class DemoSeedScript(AppDbContext db, UserManager<ApplicationUser> userManager)
{
    private static readonly Guid TenantOwnerRoleId    = Guid.Parse("00000002-0000-0000-0000-000000000001");
    private static readonly Guid MarinaManagerRoleId  = Guid.Parse("00000003-0000-0000-0000-000000000001");
    private static readonly Guid MarinaStaffRoleId    = Guid.Parse("00000004-0000-0000-0000-000000000001");
    private static readonly Guid CustomerRoleId       = Guid.Parse("00000005-0000-0000-0000-000000000001");

    public async Task<DemoSeedResult> SeedAsync(Guid tenantId, CancellationToken ct = default)
    {
        var ownerUser = await CreateUserAsync($"demo-owner-{tenantId:N}@demo.mymarina.org",
            "Demo", "Owner", "DemoPass123!", ct);
        db.UserContexts.Add(new UserContext
        {
            UserId = ownerUser.Id, RoleId = TenantOwnerRoleId, TenantId = tenantId,
        });

        // ---- Marina 1: Sunset Harbor (large commercial) ----
        var marina1 = new Marina
        {
            Id = Guid.CreateVersion7(), TenantId = tenantId,
            Name = "Sunset Harbor Marina",
            PhoneNumber = "555-0101", Email = "info@sunsetharbor.demo",
            TimeZoneId = "America/Los_Angeles",
            Address = new Domain.ValueObjects.Address("1 Harbor Way", "Marina Bay", "CA", "94000", "US"),
        };
        db.Marinas.Add(marina1);

        var mgr1 = await CreateUserAsync($"mgr1-{tenantId:N}@demo.mymarina.org", "Alex", "Marinetti", "DemoPass123!", ct);
        var staff1 = await CreateUserAsync($"staff1-{tenantId:N}@demo.mymarina.org", "Sam", "Dockhands", "DemoPass123!", ct);
        db.UserContexts.AddRange(
            new UserContext { UserId = mgr1.Id, RoleId = MarinaManagerRoleId, TenantId = tenantId, MarinaId = marina1.Id },
            new UserContext { UserId = staff1.Id, RoleId = MarinaStaffRoleId, TenantId = tenantId, MarinaId = marina1.Id });

        var dock1a = new Dock { Id = Guid.CreateVersion7(), TenantId = tenantId, MarinaId = marina1.Id, Name = "Dock A — Commercial", SortOrder = 1 };
        var dock1b = new Dock { Id = Guid.CreateVersion7(), TenantId = tenantId, MarinaId = marina1.Id, Name = "Dock B — Transient", SortOrder = 2 };
        db.Docks.AddRange(dock1a, dock1b);

        var slips1 = CreateSlips(tenantId, marina1.Id, dock1a.Id, dock1b.Id,
            ("A-01", SlipStatus.Occupied, 45), ("A-02", SlipStatus.Occupied, 40),
            ("A-03", SlipStatus.Available, 38), ("A-04", SlipStatus.Available, 42),
            ("A-05", SlipStatus.UnderMaintenance, 35), ("B-01", SlipStatus.Occupied, 50),
            ("B-02", SlipStatus.Available, 55), ("B-03", SlipStatus.Available, 30));
        db.Slips.AddRange(slips1);

        var (cust1, boat1a, boat1b) = await CreateCustomerAsync(tenantId, "Chen Family", "chen@demo.mymarina.org", ct);
        var (cust2, boat2a, _)      = await CreateCustomerAsync(tenantId, "Blue Water Charters LLC", "bwc@demo.mymarina.org", ct);
        var (cust3, boat3a, _)      = await CreateCustomerAsync(tenantId, "Rodriguez Sailboats", "rod@demo.mymarina.org", ct);

        // Slip assignments (occupied slips)
        var assign1 = new SlipAssignment { Id = Guid.CreateVersion7(), TenantId = tenantId, SlipId = slips1[0].Id, CustomerAccountId = cust1.Id, BoatId = boat1a.Id, AssignmentType = AssignmentType.Annual, StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-3)) };
        var assign2 = new SlipAssignment { Id = Guid.CreateVersion7(), TenantId = tenantId, SlipId = slips1[1].Id, CustomerAccountId = cust2.Id, BoatId = boat2a.Id, AssignmentType = AssignmentType.Monthly, StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)) };
        var assign3 = new SlipAssignment { Id = Guid.CreateVersion7(), TenantId = tenantId, SlipId = slips1[5].Id, CustomerAccountId = cust3.Id, BoatId = boat3a.Id, AssignmentType = AssignmentType.Transient, StartDate = DateOnly.FromDateTime(DateTime.UtcNow) };
        db.SlipAssignments.AddRange(assign1, assign2, assign3);

        // Invoices
        db.Invoices.AddRange(
            PaidInvoice(tenantId, marina1.Id, cust1.Id, "INV-1001", DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-2)), 1800m),
            SentInvoice(tenantId, marina1.Id, cust2.Id, "INV-1002", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)), 950m));

        // Maintenance + work order
        var maint1 = new MaintenanceRequest
        {
            Id = Guid.CreateVersion7(), TenantId = tenantId,
            CustomerAccountId = cust1.Id, SlipId = slips1[0].Id, BoatId = boat1a.Id,
            Title = "Shore power connection sparking", Description = "Pedestal at A-01 sparks when connecting 30A plug.",
            Status = MaintenanceStatus.InProgress, Priority = Priority.High,
        };
        var wo1 = new WorkOrder
        {
            Id = Guid.CreateVersion7(), TenantId = tenantId,
            MaintenanceRequestId = maint1.Id,
            Title = "Replace pedestal A-01 power outlet",
            Description = "Inspect and replace 30A outlet. Check GFCI.",
            AssignedToUserId = staff1.Id,
            Status = WorkOrderStatus.InProgress, Priority = Priority.High,
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
        };
        db.MaintenanceRequests.Add(maint1);
        db.WorkOrders.Add(wo1);

        // Announcements
        db.Announcements.AddRange(
            new Announcement
            {
                Id = Guid.CreateVersion7(), TenantId = tenantId, MarinaId = marina1.Id,
                Title = "Fuel Dock Upgrade — Closed 9–5 Tue/Wed",
                Body = "We're upgrading our fuel dock pumps this Tuesday and Wednesday. The dock will re-open Thursday morning.",
                PublishedAt = DateTimeOffset.UtcNow.AddDays(-2), IsPinned = true, CreatedByUserId = mgr1.Id,
            },
            new Announcement
            {
                Id = Guid.CreateVersion7(), TenantId = tenantId, MarinaId = marina1.Id,
                Title = "Annual Haul-Out Lottery — Sign Up Now",
                Body = "Reserve your spot for the autumn haul-out. Limited slots available.",
                PublishedAt = DateTimeOffset.UtcNow.AddDays(-5), CreatedByUserId = mgr1.Id,
            });

        // ---- Marina 2: Bayside Boatyard (small community) ----
        var marina2 = new Marina
        {
            Id = Guid.CreateVersion7(), TenantId = tenantId,
            Name = "Bayside Boatyard",
            PhoneNumber = "555-0202", Email = "yard@baysidemarina.demo",
            TimeZoneId = "America/Los_Angeles",
            Address = new Domain.ValueObjects.Address("22 Boatyard Rd", "Portside", "OR", "97401", "US"),
        };
        db.Marinas.Add(marina2);

        var mgr2 = await CreateUserAsync($"mgr2-{tenantId:N}@demo.mymarina.org", "Jordan", "Keelwood", "DemoPass123!", ct);
        db.UserContexts.Add(new UserContext { UserId = mgr2.Id, RoleId = MarinaManagerRoleId, TenantId = tenantId, MarinaId = marina2.Id });

        var dock2a = new Dock { Id = Guid.CreateVersion7(), TenantId = tenantId, MarinaId = marina2.Id, Name = "Main Float", SortOrder = 1 };
        db.Docks.Add(dock2a);

        var slips2 = CreateSlips(tenantId, marina2.Id, dock2a.Id, null,
            ("M-01", SlipStatus.Occupied, 28), ("M-02", SlipStatus.Available, 32),
            ("M-03", SlipStatus.Occupied, 25), ("M-04", SlipStatus.Available, 30),
            ("M-05", SlipStatus.UnderMaintenance, 22));
        db.Slips.AddRange(slips2);

        var (cust4, boat4a, _) = await CreateCustomerAsync(tenantId, "Park & Sons", "parks@demo.mymarina.org", ct);
        var assign4 = new SlipAssignment { Id = Guid.CreateVersion7(), TenantId = tenantId, SlipId = slips2[0].Id, CustomerAccountId = cust4.Id, BoatId = boat4a.Id, AssignmentType = AssignmentType.Monthly, StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-2)) };
        var assign5 = new SlipAssignment { Id = Guid.CreateVersion7(), TenantId = tenantId, SlipId = slips2[2].Id, CustomerAccountId = cust1.Id, BoatId = boat1b!.Id, AssignmentType = AssignmentType.Annual, StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)) };
        db.SlipAssignments.AddRange(assign4, assign5);

        db.Invoices.AddRange(
            PaidInvoice(tenantId, marina2.Id, cust4.Id, "INV-2001", DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)), 600m),
            SentInvoice(tenantId, marina2.Id, cust1.Id, "INV-2002", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)), 1200m));

        var maint2 = new MaintenanceRequest
        {
            Id = Guid.CreateVersion7(), TenantId = tenantId,
            CustomerAccountId = cust4.Id, SlipId = slips2[2].Id,
            Title = "Dock cleat missing bolt", Description = "Aft cleat on M-03 is loose — one mounting bolt missing.",
            Status = MaintenanceStatus.Submitted, Priority = Priority.Medium,
        };
        db.MaintenanceRequests.Add(maint2);

        db.Announcements.Add(new Announcement
        {
            Id = Guid.CreateVersion7(), TenantId = tenantId, MarinaId = marina2.Id,
            Title = "Welcome to the New Season!",
            Body = "Boatyard is officially open for the season. Gate code updated — check your email.",
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-7), IsPinned = true, CreatedByUserId = mgr2.Id,
        });

        await db.SaveChangesAsync(ct);

        // Return the primary customer user ID so ProvisionDemoTenantCommand can issue a customer JWT
        var customerUser = await userManager.FindByEmailAsync($"chen@demo.mymarina.org") ?? ownerUser;

        return new DemoSeedResult(
            OperatorUserId: ownerUser.Id,
            OperatorEmail: ownerUser.Email!,
            CustomerAccountId: cust1.Id,
            CustomerUserId: customerUser.Id,
            CustomerEmail: customerUser.Email!,
            Marina1Id: marina1.Id,
            Marina2Id: marina2.Id);
    }

    private async Task<ApplicationUser> CreateUserAsync(string email, string firstName, string lastName,
        string password, CancellationToken ct)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null) return existing;

        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            Email = email, UserName = email,
            FirstName = firstName, LastName = lastName, IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Failed to create demo user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        return user;
    }

    private async Task<(CustomerAccount account, Boat boat1, Boat? boat2)> CreateCustomerAsync(
        Guid tenantId, string displayName, string email, CancellationToken ct)
    {
        var user = await CreateUserAsync(email, displayName.Split(' ')[0], "Demo", "DemoPass123!", ct);

        var account = new CustomerAccount
        {
            Id = Guid.CreateVersion7(), TenantId = tenantId,
            DisplayName = displayName, BillingEmail = email, IsActive = true,
        };
        var member = new CustomerAccountMember
        {
            Id = Guid.CreateVersion7(), TenantId = tenantId,
            CustomerAccountId = account.Id, UserId = user.Id,
            Role = CustomerAccountMemberRole.Owner,
        };
        db.UserContexts.Add(new UserContext
        {
            UserId = user.Id, RoleId = CustomerRoleId, TenantId = tenantId,
            CustomerAccountId = account.Id,
        });

        var boat1 = new Boat
        {
            Id = Guid.CreateVersion7(), TenantId = tenantId, CustomerAccountId = account.Id,
            Name = $"{displayName.Split(' ')[0]}'s Vessel", Length = 35, Beam = 11, Draft = 4,
            BoatType = BoatType.Sailboat, Make = "Catalina", Year = 2018,
        };
        db.CustomerAccounts.Add(account);
        db.CustomerAccountMembers.Add(member);
        db.Boats.Add(boat1);

        // Some accounts get a second boat
        Boat? boat2 = null;
        if (displayName.Contains("Chen") || displayName.Contains("Charters"))
        {
            boat2 = new Boat
            {
                Id = Guid.CreateVersion7(), TenantId = tenantId, CustomerAccountId = account.Id,
                Name = $"{displayName.Split(' ')[0]}'s Runabout", Length = 22, Beam = 8, Draft = 2,
                BoatType = BoatType.Powerboat, Make = "Sea Ray", Year = 2020,
            };
            db.Boats.Add(boat2);
        }

        return (account, boat1, boat2);
    }

    private static List<Slip> CreateSlips(Guid tenantId, Guid marinaId, Guid dock1Id, Guid? dock2Id,
        params (string name, SlipStatus status, int length)[] specs)
    {
        var slips = new List<Slip>();
        for (var i = 0; i < specs.Length; i++)
        {
            var (name, status, length) = specs[i];
            var dockId = (dock2Id.HasValue && i >= specs.Length / 2) ? dock2Id : dock1Id;
            slips.Add(new Slip
            {
                Id = Guid.CreateVersion7(), TenantId = tenantId, MarinaId = marinaId, DockId = dockId,
                Name = name, Status = status,
                MaxLength = length, MaxBeam = 14, MaxDraft = 6,
                HasElectric = true, Electric = ElectricService.Amp30,
                HasWater = true, SlipType = SlipType.Floating,
                RateType = RateType.Flat, MonthlyRate = 300 + length * 5,
            });
        }
        return slips;
    }

    private static Invoice PaidInvoice(Guid tenantId, Guid marinaId, Guid custId,
        string number, DateOnly issued, decimal amount) => new()
    {
        Id = Guid.CreateVersion7(), TenantId = tenantId, MarinaId = marinaId,
        CustomerAccountId = custId, InvoiceNumber = number,
        Status = InvoiceStatus.Paid, IssuedDate = issued,
        DueDate = issued.AddDays(30),
        SubTotal = amount, TaxAmount = 0, TotalAmount = amount, AmountPaid = amount,
    };

    private static Invoice SentInvoice(Guid tenantId, Guid marinaId, Guid custId,
        string number, DateOnly issued, decimal amount) => new()
    {
        Id = Guid.CreateVersion7(), TenantId = tenantId, MarinaId = marinaId,
        CustomerAccountId = custId, InvoiceNumber = number,
        Status = InvoiceStatus.Sent, IssuedDate = issued,
        DueDate = issued.AddDays(30),
        SubTotal = amount, TaxAmount = 0, TotalAmount = amount, AmountPaid = 0,
    };
}

public sealed record DemoSeedResult(
    Guid OperatorUserId,
    string OperatorEmail,
    Guid CustomerAccountId,
    Guid CustomerUserId,
    string CustomerEmail,
    Guid Marina1Id,
    Guid Marina2Id);
