using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Domain.ValueObjects;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Demo;

public class DemoSeedScript(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    ILogger<DemoSeedScript> logger)
{
    // ── Well-known IDs — stable across re-deploys ─────────────────────────────
    public static readonly Guid TenantId    = new("d0000000-0000-0000-0000-000000000001");
    public static readonly Guid DemoUserId  = new("d0000000-0000-0000-0000-000000000002");

    public static readonly Guid SunsetBayId   = new("d0000000-0000-0000-0000-000000000010");
    public static readonly Guid YachtClubId   = new("d0000000-0000-0000-0000-000000000011");
    public static readonly Guid DockoId       = new("d0000000-0000-0000-0000-000000000012");
    public static readonly Guid PrivateDockId = new("d0000000-0000-0000-0000-000000000013");

    static readonly Guid DockAId  = new("d0000000-0000-0000-0000-000000000020");
    static readonly Guid DockBId  = new("d0000000-0000-0000-0000-000000000021");
    static readonly Guid YCDockId = new("d0000000-0000-0000-0000-000000000022");

    static readonly Guid SlipA1Id     = new("d0000000-0000-0000-0000-000000000030");
    static readonly Guid SlipA2Id     = new("d0000000-0000-0000-0000-000000000031");
    static readonly Guid SlipA3Id     = new("d0000000-0000-0000-0000-000000000032");
    static readonly Guid SlipA4Id     = new("d0000000-0000-0000-0000-000000000033");
    static readonly Guid SlipA5Id     = new("d0000000-0000-0000-0000-000000000034");
    static readonly Guid SlipA6Id     = new("d0000000-0000-0000-0000-000000000035");
    static readonly Guid SlipB1Id     = new("d0000000-0000-0000-0000-000000000036");
    static readonly Guid SlipB2Id     = new("d0000000-0000-0000-0000-000000000037");
    static readonly Guid SlipB3Id     = new("d0000000-0000-0000-0000-000000000038");
    static readonly Guid SlipB4Id     = new("d0000000-0000-0000-0000-000000000039");
    static readonly Guid SlipYC1Id    = new("d0000000-0000-0000-0000-000000000040");
    static readonly Guid SlipDockoId  = new("d0000000-0000-0000-0000-000000000041");
    static readonly Guid SlipPrivateId = new("d0000000-0000-0000-0000-000000000042");

    static readonly Guid AccountJohnsonId  = new("d0000000-0000-0000-0000-000000000050");
    static readonly Guid AccountWilliamsId = new("d0000000-0000-0000-0000-000000000051");
    static readonly Guid AccountChenId     = new("d0000000-0000-0000-0000-000000000052");

    static readonly Guid VesselSummerWindId  = new("d0000000-0000-0000-0000-000000000060");
    static readonly Guid VesselBlueHorizonId = new("d0000000-0000-0000-0000-000000000061");
    static readonly Guid VesselSeaBreezeId   = new("d0000000-0000-0000-0000-000000000062");
    static readonly Guid VesselMakoId        = new("d0000000-0000-0000-0000-000000000063");
    static readonly Guid VesselPearlId       = new("d0000000-0000-0000-0000-000000000064");

    static readonly Guid AssignmentA1Id = new("d0000000-0000-0000-0000-000000000070");
    static readonly Guid AssignmentA3Id = new("d0000000-0000-0000-0000-000000000071");
    static readonly Guid AssignmentB3Id = new("d0000000-0000-0000-0000-000000000072");

    static readonly Guid WindowA2Id = new("d0000000-0000-0000-0000-000000000080");
    static readonly Guid WindowA4Id = new("d0000000-0000-0000-0000-000000000081");
    static readonly Guid WindowB1Id = new("d0000000-0000-0000-0000-000000000082");
    static readonly Guid WindowB2Id = new("d0000000-0000-0000-0000-000000000083");

    static readonly Guid ReservationConfirmedId = new("d0000000-0000-0000-0000-000000000090");
    static readonly Guid ReservationPendingId   = new("d0000000-0000-0000-0000-000000000091");
    static readonly Guid ReservationCompletedId = new("d0000000-0000-0000-0000-000000000092");
    static readonly Guid ReservationCancelledId = new("d0000000-0000-0000-0000-000000000093");

    static readonly Guid InvoiceDraftId   = new("d0000000-0000-0000-0000-0000000000a0");
    static readonly Guid InvoiceSentId    = new("d0000000-0000-0000-0000-0000000000a1");
    static readonly Guid InvoicePaidId    = new("d0000000-0000-0000-0000-0000000000a2");
    static readonly Guid InvoiceOverdueId = new("d0000000-0000-0000-0000-0000000000a3");

    static readonly Guid MaintReq1Id = new("d0000000-0000-0000-0000-0000000000b0");
    static readonly Guid MaintReq2Id = new("d0000000-0000-0000-0000-0000000000b1");
    static readonly Guid MaintReq3Id = new("d0000000-0000-0000-0000-0000000000b2");

    static readonly Guid WorkOrder1Id = new("d0000000-0000-0000-0000-0000000000c0");
    static readonly Guid WorkOrder2Id = new("d0000000-0000-0000-0000-0000000000c1");
    static readonly Guid WorkOrder3Id = new("d0000000-0000-0000-0000-0000000000c2");

    static readonly Guid Announce1Id = new("d0000000-0000-0000-0000-0000000000d0");
    static readonly Guid Announce2Id = new("d0000000-0000-0000-0000-0000000000d1");
    static readonly Guid Announce3Id = new("d0000000-0000-0000-0000-0000000000d2");

    // ─────────────────────────────────────────────────────────────────────────

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await db.Tenants.AnyAsync(t => t.IsDemo, ct))
        {
            logger.LogInformation("Demo tenant already seeded — skipping");
            return;
        }

        logger.LogInformation("Seeding demo tenant...");
        await SeedDemoUserAsync(ct);
        await SeedEntitiesAsync(ct);
        logger.LogInformation("Demo seed complete");
    }

    private async Task SeedDemoUserAsync(CancellationToken ct)
    {
        if (await userManager.FindByIdAsync(DemoUserId.ToString()) is not null) return;

        var user = new ApplicationUser
        {
            Id             = DemoUserId,
            UserName       = "demo@mymarina.org",
            Email          = "demo@mymarina.org",
            FirstName      = "Demo",
            LastName       = "User",
            EmailConfirmed = true,
        };
        var result = await userManager.CreateAsync(user, "Demo@Marina123!");
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Failed to create demo user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
    }

    private async Task SeedEntitiesAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var now   = DateTimeOffset.UtcNow;

        // ── Tenant ─────────────────────────────────────────────────────────────
        db.Tenants.Add(new Tenant
        {
            Id               = TenantId,
            Name             = "MyMarina Demo",
            Slug             = "demo",
            SubscriptionTier = SubscriptionTier.Pro,
            IsDemo           = true,
            BillingEmail     = "demo@mymarina.org",
        });

        // ── Marinas ────────────────────────────────────────────────────────────
        db.Marinas.AddRange(
            new Marina
            {
                Id             = SunsetBayId,
                TenantId       = TenantId,
                Name           = "Sunset Bay Marina",
                Slug           = "sunset-bay",
                MarinaType     = MarinaType.Commercial,
                AddressStreet  = "100 Marina Way",
                AddressCity    = "Newport",
                AddressState   = "RI",
                AddressZip     = "02840",
                AddressCountry = "US",
                Latitude       = 41.4901m,
                Longitude      = -71.3128m,
                PhoneNumber    = "(401) 555-0100",
                Email          = "info@sunsetbay.demo",
                Website        = "https://sunsetbay.demo",
                Description    = "Newport's premier full-service marina offering 120 slips, a fuel dock, pump-out station, and concierge services.",
                TimeZoneId     = "America/New_York",
                IsListed       = true,
            },
            new Marina
            {
                Id             = YachtClubId,
                TenantId       = TenantId,
                Name           = "Eastside Yacht Club",
                Slug           = "eastside-yc",
                MarinaType     = MarinaType.YachtClub,
                AddressStreet  = "55 Sail Club Drive",
                AddressCity    = "Middletown",
                AddressState   = "RI",
                AddressZip     = "02842",
                AddressCountry = "US",
                Latitude       = 41.5203m,
                Longitude      = -71.2775m,
                PhoneNumber    = "(401) 555-0200",
                Email          = "info@eastsideyc.demo",
                Description    = "Members-only yacht club with 30 deep-water slips and a full race program.",
                TimeZoneId     = "America/New_York",
                IsListed       = true,
            },
            new Marina
            {
                Id         = DockoId,
                TenantId   = TenantId,
                Name       = "Maria's Slip at Sunset Bay",
                Slug       = "marias-slip",
                MarinaType = MarinaType.Dockominium,
                Latitude   = 41.4901m,
                Longitude  = -71.3128m,
                TimeZoneId = "America/New_York",
                IsListed   = true,
            },
            new Marina
            {
                Id           = PrivateDockId,
                TenantId     = TenantId,
                Name         = "Captain's Cove",
                Slug         = "captains-cove",
                MarinaType   = MarinaType.PrivateDock,
                AddressCity  = "Portsmouth",
                AddressState = "RI",
                Latitude     = 41.6038m,
                Longitude    = -71.2632m,
                TimeZoneId   = "America/New_York",
                IsListed     = true,
            }
        );

        // ── Memberships (demo user owns Sunset Bay, manages Eastside YC) ───────
        db.Memberships.AddRange(
            new Membership
            {
                Id         = new Guid("d0000000-0000-0000-0000-000000000100"),
                UserId     = DemoUserId,
                TenantId   = TenantId,
                MarinaId   = SunsetBayId,
                Scope      = MembershipScope.Marina,
                Role       = MembershipRole.Owner,
                AcceptedAt = now.AddMonths(-6),
            },
            new Membership
            {
                Id         = new Guid("d0000000-0000-0000-0000-000000000101"),
                UserId     = DemoUserId,
                TenantId   = TenantId,
                MarinaId   = YachtClubId,
                Scope      = MembershipScope.Marina,
                Role       = MembershipRole.Manager,
                AcceptedAt = now.AddMonths(-6),
            }
        );

        // ── Docks ──────────────────────────────────────────────────────────────
        db.Docks.AddRange(
            new Dock { Id = DockAId,  MarinaId = SunsetBayId,  Name = "Dock A",        Description = "Main floating dock" },
            new Dock { Id = DockBId,  MarinaId = SunsetBayId,  Name = "Dock B",        Description = "Seasonal dock" },
            new Dock { Id = YCDockId, MarinaId = YachtClubId,  Name = "Members' Dock" }
        );

        // ── Slips ──────────────────────────────────────────────────────────────
        db.Slips.AddRange(
            Slip(SlipA1Id,     SunsetBayId,  DockAId,  "A-1",            50, 17, 8,  true, ElectricAmperage.Amp50, true,  transientRate: 3.50m, lat: 41.4902m, lon: -71.3127m),
            Slip(SlipA2Id,     SunsetBayId,  DockAId,  "A-2",            48, 16, 7,  true, ElectricAmperage.Amp30, true,  transientRate: 3.25m, lat: 41.4902m, lon: -71.3126m),
            Slip(SlipA3Id,     SunsetBayId,  DockAId,  "A-3",            50, 17, 8,  true, ElectricAmperage.Amp50, true,  transientRate: 3.50m, lat: 41.4902m, lon: -71.3125m),
            Slip(SlipA4Id,     SunsetBayId,  DockAId,  "A-4",            45, 15, 6,  true, ElectricAmperage.Amp30, true,  transientRate: 3.00m, lat: 41.4903m, lon: -71.3124m),
            Slip(SlipA5Id,     SunsetBayId,  DockAId,  "A-5",            38, 14, 5,  true, ElectricAmperage.Amp30, true,  transientRate: 2.75m, lat: 41.4903m, lon: -71.3123m),
            Slip(SlipA6Id,     SunsetBayId,  DockAId,  "A-6",            36, 13, 5,  true, ElectricAmperage.Amp30, true,  transientRate: 2.75m, lat: 41.4903m, lon: -71.3122m),
            Slip(SlipB1Id,     SunsetBayId,  DockBId,  "B-1",            40, 14, 5,  true, ElectricAmperage.Amp30, true,  transientRate: 2.80m, lat: 41.4904m, lon: -71.3130m),
            Slip(SlipB2Id,     SunsetBayId,  DockBId,  "B-2",            38, 14, 5,  true, ElectricAmperage.Amp30, true,  transientRate: 2.80m, lat: 41.4904m, lon: -71.3129m),
            Slip(SlipB3Id,     SunsetBayId,  DockBId,  "B-3",            28, 10, 4,  false, null,                 true,  leaseRate: 18m, leaseTerm: LeaseTerm.Monthly,  lat: 41.4904m, lon: -71.3128m),
            Slip(SlipB4Id,     SunsetBayId,  DockBId,  "B-4",            30, 11, 4,  false, null,                 true,  leaseRate: 18m, leaseTerm: LeaseTerm.Monthly,  lat: 41.4905m, lon: -71.3128m),
            Slip(SlipYC1Id,    YachtClubId,  YCDockId, "P-1",            65, 22, 9,  true, ElectricAmperage.Amp50, true,  leaseRate: 24m,  leaseTerm: LeaseTerm.Annual,  lat: 41.5204m, lon: -71.2774m),
            new Slip
            {
                Id = SlipDockoId, MarinaId = DockoId, HostMarinaId = SunsetBayId,
                HostMarinaPolicy = HostMarinaPolicy.NotifyOnly,
                Name = "Maria's Slip", SlipType = SlipType.Floating,
                MaxLength = 44, MaxBeam = 15, MaxDraft = 6,
                HasElectric = true, Electric = ElectricAmperage.Amp30, HasWater = true,
                Status = SlipStatus.Active,
                DefaultTransientRateKind = RateKind.Flat, DefaultTransientBaseRate = 95m,
                Latitude = 41.4901m, Longitude = -71.3128m,
            },
            new Slip
            {
                Id = SlipPrivateId, MarinaId = PrivateDockId,
                Name = "Captain's Slip", SlipType = SlipType.Fixed,
                MaxLength = 34, MaxBeam = 12, MaxDraft = 4,
                HasElectric = true, Electric = ElectricAmperage.Amp30, HasWater = true,
                Status = SlipStatus.Active,
                DefaultTransientRateKind = RateKind.Flat, DefaultTransientBaseRate = 75m,
                Latitude = 41.6039m, Longitude = -71.2631m,
            }
        );

        // ── Billing Accounts ───────────────────────────────────────────────────
        db.BillingAccounts.AddRange(
            new BillingAccount
            {
                Id = AccountJohnsonId, MarinaId = SunsetBayId,
                DisplayName = "Johnson Family", BillingEmail = "john.johnson@email.demo",
                BillingPhone = "(401) 555-1001",
                BillingAddressStreet = "22 Harbor Hill Rd", BillingAddressCity = "Newport",
                BillingAddressState = "RI", BillingAddressZip = "02840",
                EmergencyContactName = "Jane Johnson", EmergencyContactPhone = "(401) 555-1002",
            },
            new BillingAccount
            {
                Id = AccountWilliamsId, MarinaId = SunsetBayId,
                DisplayName = "Williams Marine LLC", BillingEmail = "contact@williams-marine.demo",
                BillingPhone = "(401) 555-2001",
            },
            new BillingAccount
            {
                Id = AccountChenId, MarinaId = SunsetBayId,
                DisplayName = "Sarah Chen", BillingEmail = "sarah.chen@email.demo",
                BillingPhone = "(401) 555-3001",
            }
        );

        // ── Billing Account Members ────────────────────────────────────────────
        db.BillingAccountMembers.AddRange(
            new BillingAccountMember { Id = new Guid("d0000000-0000-0000-0000-000000000110"), BillingAccountId = AccountJohnsonId,  UserId = DemoUserId, Role = BillingAccountRole.Owner,  AcceptedAt = now.AddMonths(-6) },
            new BillingAccountMember { Id = new Guid("d0000000-0000-0000-0000-000000000111"), BillingAccountId = AccountWilliamsId, UserId = DemoUserId, Role = BillingAccountRole.Member, AcceptedAt = now.AddMonths(-5) },
            new BillingAccountMember { Id = new Guid("d0000000-0000-0000-0000-000000000112"), BillingAccountId = AccountChenId,     UserId = DemoUserId, Role = BillingAccountRole.Member, AcceptedAt = now.AddMonths(-4) }
        );

        // ── Vessels ────────────────────────────────────────────────────────────
        db.Vessels.AddRange(
            new Vessel { Id = VesselSummerWindId,  OwnerUserId = DemoUserId, Name = "Summer Wind",   Make = "Catalina",      Model = "445",         Year = 2018, BoatType = BoatType.Sailboat,   Length = 44m,  Beam = 13.5m, Draft = 6.5m, HullColor = "White",      RegistrationNumber = "RI-1234-AB", RegistrationState = "RI" },
            new Vessel { Id = VesselBlueHorizonId, OwnerUserId = DemoUserId, Name = "Blue Horizon",  Make = "Sea Ray",       Model = "400 SLX",     Year = 2020, BoatType = BoatType.Powerboat,  Length = 40m,  Beam = 14m,   Draft = 3.5m, HullColor = "Blue/White", RegistrationNumber = "RI-5678-CD", RegistrationState = "RI" },
            new Vessel { Id = VesselSeaBreezeId,   OwnerUserId = DemoUserId, Name = "Sea Breeze",    Make = "Beneteau",      Model = "Oceanis 38",  Year = 2016, BoatType = BoatType.Sailboat,   Length = 38m,  Beam = 13m,   Draft = 6m,   HullColor = "White/Blue", RegistrationNumber = "RI-2468-EF", RegistrationState = "RI" },
            new Vessel { Id = VesselMakoId,        OwnerUserId = DemoUserId, Name = "Mako Express",  Make = "Boston Whaler", Model = "280 Outrage", Year = 2021, BoatType = BoatType.Powerboat,  Length = 28m,  Beam = 10m,   Draft = 2.5m, HullColor = "White",      RegistrationNumber = "RI-1357-GH", RegistrationState = "RI" },
            new Vessel { Id = VesselPearlId,       OwnerUserId = null,       Name = "Pearl",         Make = "Leopard",       Model = "40",          Year = 2019, BoatType = BoatType.Catamaran,  Length = 40m,  Beam = 23m,   Draft = 3m,   HullColor = "White",      ClaimEmail = "sarah.chen@email.demo" }
        );

        // ── Marina Vessel Records ──────────────────────────────────────────────
        db.MarinaVesselRecords.AddRange(
            new MarinaVesselRecord { Id = new Guid("d0000000-0000-0000-0000-000000000120"), MarinaId = SunsetBayId, VesselId = VesselSummerWindId,  BillingAccountId = AccountJohnsonId,  InsuranceProvider = "BoatUS",      InsurancePolicyNumber = "BAM-001-2025", InsuranceExpiresOn = today.AddYears(1) },
            new MarinaVesselRecord { Id = new Guid("d0000000-0000-0000-0000-000000000121"), MarinaId = SunsetBayId, VesselId = VesselBlueHorizonId, BillingAccountId = AccountJohnsonId,  InsuranceProvider = "Progressive", InsurancePolicyNumber = "PRG-002-2025", InsuranceExpiresOn = today.AddYears(1) },
            new MarinaVesselRecord { Id = new Guid("d0000000-0000-0000-0000-000000000122"), MarinaId = SunsetBayId, VesselId = VesselSeaBreezeId,   BillingAccountId = AccountWilliamsId, InsuranceProvider = "BoatUS",      InsurancePolicyNumber = "BAM-003-2025", InsuranceExpiresOn = today.AddMonths(9) },
            new MarinaVesselRecord { Id = new Guid("d0000000-0000-0000-0000-000000000123"), MarinaId = SunsetBayId, VesselId = VesselMakoId,        BillingAccountId = AccountWilliamsId, InsuranceProvider = "Markel",      InsurancePolicyNumber = "MKL-004-2025", InsuranceExpiresOn = today.AddMonths(10) },
            new MarinaVesselRecord { Id = new Guid("d0000000-0000-0000-0000-000000000124"), MarinaId = SunsetBayId, VesselId = VesselPearlId,       BillingAccountId = AccountChenId,     Notes = "Ghost vessel — pending claim by Sarah Chen." }
        );

        // ── Slip Assignments ───────────────────────────────────────────────────
        db.SlipAssignments.AddRange(
            new SlipAssignment
            {
                Id = AssignmentA1Id, SlipId = SlipA1Id, BillingAccountId = AccountJohnsonId, VesselId = VesselSummerWindId,
                AssignmentType = AssignmentType.Annual, StartDate = today.AddMonths(-3), BaseRate = 50 * 24m,
                AllowOwnerSubletWhenAway = true, AllowHolderSublet = false,
                OwnerSubletShareToHolder = 0.40m, HolderSubletShareToOwner = 0m,
            },
            new SlipAssignment
            {
                Id = AssignmentA3Id, SlipId = SlipA3Id, BillingAccountId = AccountWilliamsId, VesselId = VesselSeaBreezeId,
                AssignmentType = AssignmentType.Annual, StartDate = today.AddMonths(-8), EndDate = today.AddMonths(4), BaseRate = 48 * 24m,
                AllowOwnerSubletWhenAway = true, AllowHolderSublet = true,
                OwnerSubletShareToHolder = 0.35m, HolderSubletShareToOwner = 0.65m,
            },
            new SlipAssignment
            {
                Id = AssignmentB3Id, SlipId = SlipB3Id, BillingAccountId = AccountChenId, VesselId = VesselPearlId,
                AssignmentType = AssignmentType.Monthly, StartDate = today.AddMonths(-2), BaseRate = 28 * 18m,
                AllowOwnerSubletWhenAway = false, AllowHolderSublet = false,
                OwnerSubletShareToHolder = 0m, HolderSubletShareToOwner = 0m,
            }
        );

        // ── Availability Windows ───────────────────────────────────────────────
        var splitOwner = new List<RevenueSplitEntry>
        {
            new() { PayeeKind = "SlipOwner", PayeeId = SunsetBayId, Percent = 1m }
        };

        db.AvailabilityWindows.AddRange(
            new AvailabilityWindow
            {
                Id = WindowA2Id, SlipId = SlipA2Id,
                ListedByKind = ListedByKind.Owner, ListedByMarinaId = SunsetBayId,
                ListingKind = ListingKind.Transient,
                StartsAt = now.AddDays(-30), EndsAt = now.AddDays(180),
                InstantBook = true, MinNights = 1, MaxNights = 30,
                RateKind = RateKind.PerFoot, BasePricePerNight = 3.25m,
                CleaningFee = 35m, WeeklyDiscount = 0.10m, MonthlyDiscount = 0.20m,
                RevenueSplit = splitOwner,
                Status = AvailabilityWindowStatus.Open,
            },
            new AvailabilityWindow
            {
                Id = WindowA4Id, SlipId = SlipA4Id,
                ListedByKind = ListedByKind.Owner, ListedByMarinaId = SunsetBayId,
                ListingKind = ListingKind.Transient,
                StartsAt = now.AddDays(7), EndsAt = now.AddDays(120),
                InstantBook = false, MinNights = 2, MaxNights = 14,
                RateKind = RateKind.PerFoot, BasePricePerNight = 3.00m,
                CleaningFee = 35m,
                RevenueSplit = splitOwner,
                Status = AvailabilityWindowStatus.Open,
            },
            new AvailabilityWindow
            {
                Id = WindowB1Id, SlipId = SlipB1Id,
                ListedByKind = ListedByKind.Owner, ListedByMarinaId = SunsetBayId,
                ListingKind = ListingKind.Transient,
                StartsAt = now.AddDays(-60), EndsAt = now.AddDays(90),
                InstantBook = true, MinNights = 1,
                RateKind = RateKind.PerFoot, BasePricePerNight = 2.80m,
                RevenueSplit = splitOwner,
                Status = AvailabilityWindowStatus.Open,
            },
            new AvailabilityWindow
            {
                Id = WindowB2Id, SlipId = SlipB2Id,
                ListedByKind = ListedByKind.Owner, ListedByMarinaId = SunsetBayId,
                ListingKind = ListingKind.Transient,
                StartsAt = now.AddDays(-90), EndsAt = now.AddDays(60),
                InstantBook = true, MinNights = 1,
                RateKind = RateKind.PerFoot, BasePricePerNight = 2.80m,
                RevenueSplit = splitOwner,
                Status = AvailabilityWindowStatus.Open,
            }
        );

        // ── Reservations ───────────────────────────────────────────────────────
        var splitSnap = new List<RevenueSplitEntry>
        {
            new() { PayeeKind = "SlipOwner", PayeeId = SunsetBayId, Percent = 1m }
        };

        db.Reservations.AddRange(
            new Reservation
            {
                Id = ReservationConfirmedId,
                BoaterUserId = DemoUserId, VesselId = VesselBlueHorizonId, SlipId = SlipB1Id, AvailabilityWindowId = WindowB1Id,
                ArrivesAt = now.AddDays(14), DepartsAt = now.AddDays(17),
                Status = ReservationStatus.Confirmed,
                BasePrice = 3 * 40 * 2.80m, Fees = 35m, Taxes = 0m, Total = 3 * 40 * 2.80m + 35m,
                RevenueSplitSnapshot = splitSnap,
                ConfirmedAt = now.AddDays(-2),
                PaymentStatus = PaymentStatus.OffPlatform,
            },
            new Reservation
            {
                Id = ReservationPendingId,
                BoaterUserId = DemoUserId, VesselId = VesselBlueHorizonId, SlipId = SlipA4Id, AvailabilityWindowId = WindowA4Id,
                ArrivesAt = now.AddDays(30), DepartsAt = now.AddDays(33),
                Status = ReservationStatus.PendingApproval,
                BasePrice = 3 * 45 * 3.00m, Fees = 35m, Taxes = 0m, Total = 3 * 45 * 3.00m + 35m,
                RevenueSplitSnapshot = splitSnap,
                PaymentStatus = PaymentStatus.OffPlatform,
            },
            new Reservation
            {
                Id = ReservationCompletedId,
                BoaterUserId = DemoUserId, VesselId = VesselSummerWindId, SlipId = SlipA2Id, AvailabilityWindowId = WindowA2Id,
                ArrivesAt = now.AddDays(-20), DepartsAt = now.AddDays(-17),
                Status = ReservationStatus.Completed,
                BasePrice = 3 * 44 * 3.25m, Fees = 35m, Taxes = 0m, Total = 3 * 44 * 3.25m + 35m,
                RevenueSplitSnapshot = splitSnap,
                ConfirmedAt = now.AddDays(-25),
                PaymentStatus = PaymentStatus.OffPlatform,
            },
            new Reservation
            {
                Id = ReservationCancelledId,
                BoaterUserId = DemoUserId, VesselId = VesselBlueHorizonId, SlipId = SlipB2Id, AvailabilityWindowId = WindowB2Id,
                ArrivesAt = now.AddDays(-5), DepartsAt = now.AddDays(-3),
                Status = ReservationStatus.Cancelled,
                BasePrice = 2 * 40 * 2.80m, Fees = 35m, Taxes = 0m, Total = 2 * 40 * 2.80m + 35m,
                RevenueSplitSnapshot = splitSnap,
                CancelledAt = now.AddDays(-10), CancelledByUserId = DemoUserId,
                PaymentStatus = PaymentStatus.OffPlatform,
            }
        );

        // ── Invoices ───────────────────────────────────────────────────────────
        db.Invoices.AddRange(
            new Invoice
            {
                Id = InvoiceDraftId, MarinaId = SunsetBayId, BillingAccountId = AccountJohnsonId,
                SlipAssignmentId = AssignmentA1Id,
                InvoiceNumber = "DEMO-001", Status = InvoiceStatus.Draft,
                IssuedDate = today, DueDate = today.AddDays(30),
                SubTotal = 50 * 24m, TaxAmount = 0m, TotalAmount = 50 * 24m, AmountPaid = 0m,
                Notes = "Annual slip fee — A-1",
            },
            new Invoice
            {
                Id = InvoiceSentId, MarinaId = SunsetBayId, BillingAccountId = AccountWilliamsId,
                SlipAssignmentId = AssignmentA3Id,
                InvoiceNumber = "DEMO-002", Status = InvoiceStatus.Sent,
                IssuedDate = today.AddDays(-10), DueDate = today.AddDays(20),
                SubTotal = 48 * 24m, TaxAmount = 0m, TotalAmount = 48 * 24m, AmountPaid = 0m,
                Notes = "Annual slip fee — A-3",
            },
            new Invoice
            {
                Id = InvoicePaidId, MarinaId = SunsetBayId, BillingAccountId = AccountChenId,
                SlipAssignmentId = AssignmentB3Id,
                InvoiceNumber = "DEMO-003", Status = InvoiceStatus.Paid,
                IssuedDate = today.AddDays(-45), DueDate = today.AddDays(-15),
                SubTotal = 28 * 18m, TaxAmount = 0m, TotalAmount = 28 * 18m, AmountPaid = 28 * 18m,
                Notes = "Monthly slip fee — B-3",
            },
            new Invoice
            {
                Id = InvoiceOverdueId, MarinaId = SunsetBayId, BillingAccountId = AccountJohnsonId,
                InvoiceNumber = "DEMO-004", Status = InvoiceStatus.Overdue,
                IssuedDate = today.AddDays(-60), DueDate = today.AddDays(-30),
                SubTotal = 350m, TaxAmount = 0m, TotalAmount = 350m, AmountPaid = 0m,
                Notes = "Fuel and pump-out charges",
            }
        );

        // ── Invoice Line Items ─────────────────────────────────────────────────
        db.InvoiceLineItems.AddRange(
            new InvoiceLineItem { Id = new Guid("d0000000-0000-0000-0000-0000000000e0"), InvoiceId = InvoiceDraftId,   Description = "Annual Slip Lease — A-1 (50 ft)",   Quantity = 1, UnitPrice = 50 * 24m, LineTotal = 50 * 24m },
            new InvoiceLineItem { Id = new Guid("d0000000-0000-0000-0000-0000000000e1"), InvoiceId = InvoiceSentId,    Description = "Annual Slip Lease — A-3 (48 ft)",   Quantity = 1, UnitPrice = 48 * 24m, LineTotal = 48 * 24m },
            new InvoiceLineItem { Id = new Guid("d0000000-0000-0000-0000-0000000000e2"), InvoiceId = InvoicePaidId,    Description = "Monthly Slip Lease — B-3 (28 ft)",  Quantity = 1, UnitPrice = 28 * 18m, LineTotal = 28 * 18m },
            new InvoiceLineItem { Id = new Guid("d0000000-0000-0000-0000-0000000000e3"), InvoiceId = InvoiceOverdueId, Description = "Fuel dock service",                 Quantity = 1, UnitPrice = 250m,      LineTotal = 250m },
            new InvoiceLineItem { Id = new Guid("d0000000-0000-0000-0000-0000000000e4"), InvoiceId = InvoiceOverdueId, Description = "Pump-out service",                  Quantity = 2, UnitPrice = 50m,       LineTotal = 100m }
        );

        // ── Payments ───────────────────────────────────────────────────────────
        db.Payments.Add(new Payment
        {
            Id = new Guid("d0000000-0000-0000-0000-0000000000f0"),
            InvoiceId = InvoicePaidId, Amount = 28 * 18m,
            PaidOn = today.AddDays(-20), Method = PaymentMethod.Check,
            ReferenceNumber = "CHK-4821", RecordedByUserId = DemoUserId,
        });

        // ── Maintenance Requests ───────────────────────────────────────────────
        db.MaintenanceRequests.AddRange(
            new MaintenanceRequest
            {
                Id = MaintReq1Id, MarinaId = SunsetBayId, BoaterUserId = DemoUserId,
                VesselId = VesselSummerWindId, SlipId = SlipA1Id, BillingAccountId = AccountJohnsonId,
                Title = "Electrical outlet not working at A-1",
                Description = "The 50-amp pedestal at slip A-1 has no power on the top outlet. Lower outlet works fine.",
                Status = MaintenanceRequestStatus.Submitted, Priority = MaintenancePriority.High,
            },
            new MaintenanceRequest
            {
                Id = MaintReq2Id, MarinaId = SunsetBayId, BoaterUserId = DemoUserId,
                SlipId = SlipB2Id, BillingAccountId = AccountWilliamsId,
                Title = "Water hose leaking at Dock B",
                Description = "The water connection fitting at B-2 is dripping continuously. Started 2 days ago.",
                Status = MaintenanceRequestStatus.InProgress, Priority = MaintenancePriority.Medium,
            },
            new MaintenanceRequest
            {
                Id = MaintReq3Id, MarinaId = SunsetBayId, BoaterUserId = DemoUserId,
                SlipId = SlipA3Id, BillingAccountId = AccountWilliamsId,
                Title = "Dock light out near A-3",
                Description = "The overhead dock light between A-3 and A-4 is not working at night.",
                Status = MaintenanceRequestStatus.Completed, Priority = MaintenancePriority.Low,
                ResolvedAt = now.AddDays(-5),
            }
        );

        // ── Work Orders ────────────────────────────────────────────────────────
        db.WorkOrders.AddRange(
            new WorkOrder
            {
                Id = WorkOrder1Id, MarinaId = SunsetBayId, MaintenanceRequestId = MaintReq1Id,
                Title = "Repair 50-amp outlet pedestal A-1",
                Description = "Inspect and replace faulty GFCI outlet on A-1 pedestal. Check breaker panel.",
                Status = WorkOrderStatus.Open, Priority = MaintenancePriority.High,
                ScheduledDate = today.AddDays(2), AssignedToUserId = DemoUserId,
            },
            new WorkOrder
            {
                Id = WorkOrder2Id, MarinaId = SunsetBayId, MaintenanceRequestId = MaintReq2Id,
                Title = "Replace water fitting at B-2",
                Description = "Remove and replace the 3/4\" brass fitting at Dock B hose bib #2.",
                Status = WorkOrderStatus.InProgress, Priority = MaintenancePriority.Medium,
                ScheduledDate = today, AssignedToUserId = DemoUserId,
            },
            new WorkOrder
            {
                Id = WorkOrder3Id, MarinaId = SunsetBayId,
                Title = "Seasonal fuel dock inspection",
                Description = "Annual safety and compliance inspection of all fuel dock equipment.",
                Status = WorkOrderStatus.Open, Priority = MaintenancePriority.Medium,
                ScheduledDate = today.AddDays(14),
            }
        );

        // ── Announcements ──────────────────────────────────────────────────────
        db.Announcements.AddRange(
            new Announcement
            {
                Id = Announce1Id, MarinaId = SunsetBayId, CreatedByUserId = DemoUserId,
                Title = "Welcome to the 2026 Season!",
                Body = "We're thrilled to welcome you back to Sunset Bay Marina. Gate codes have been updated — check your email for the new code. The fuel dock opens May 1st at 7 AM.",
                Audience = AnnouncementAudience.Both, IsPinned = true,
                PublishedAt = now.AddDays(-14),
            },
            new Announcement
            {
                Id = Announce2Id, MarinaId = SunsetBayId, CreatedByUserId = DemoUserId,
                Title = "Dock B Maintenance — May 10–11",
                Body = "Dock B will be closed for scheduled maintenance on May 10–11 from 8 AM to 5 PM. Temporary side-tie arrangements are available — contact the dock office.",
                Audience = AnnouncementAudience.IncomingBoaters, IsPinned = false,
                PublishedAt = now.AddDays(-5), ExpiresAt = now.AddDays(10),
            },
            new Announcement
            {
                Id = Announce3Id, MarinaId = SunsetBayId, CreatedByUserId = DemoUserId,
                Title = "Staff Meeting — Monday 8 AM",
                Body = "All marina staff: mandatory safety briefing Monday at 8 AM in the dock office. Topics: fire extinguisher locations, new fuel dock procedures, and emergency contact list updates.",
                Audience = AnnouncementAudience.Customers, IsPinned = false,
                PublishedAt = now.AddDays(-3),
            }
        );

        await db.SaveChangesAsync(ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static Slip Slip(
        Guid id, Guid marinaId, Guid dockId, string name,
        decimal maxLength, decimal maxBeam, decimal maxDraft,
        bool hasElectric, ElectricAmperage? electric, bool hasWater,
        decimal? transientRate = null, decimal? leaseRate = null, LeaseTerm? leaseTerm = null,
        decimal? lat = null, decimal? lon = null)
        => new()
        {
            Id = id, MarinaId = marinaId, DockId = dockId, Name = name,
            SlipType = SlipType.Floating, MaxLength = maxLength, MaxBeam = maxBeam, MaxDraft = maxDraft,
            HasElectric = hasElectric, Electric = electric, HasWater = hasWater,
            Status = SlipStatus.Active,
            DefaultTransientRateKind = transientRate.HasValue ? RateKind.PerFoot : null,
            DefaultTransientBaseRate = transientRate,
            DefaultLeaseRateKind     = leaseRate.HasValue ? RateKind.PerFoot : null,
            DefaultLeaseBaseRate     = leaseRate,
            DefaultLeaseTerm         = leaseTerm,
            Latitude  = lat,
            Longitude = lon,
        };
}
