using MyMarina.Application.Marinas;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Marinas;

internal static class MarinaMappers
{
    internal static TenantDto ToTenantDto(Tenant t) => new(
        Id: t.Id,
        Name: t.Name,
        Slug: t.Slug,
        SubscriptionTier: t.SubscriptionTier.ToString(),
        IsActive: t.IsActive,
        CreatedAt: t.CreatedAt
    );

    internal static MarinaDto ToMarinaDto(Marina m, string? logoUrl = null, string? bannerThumbnailUrl = null) => new(
        Id: m.Id,
        TenantId: m.TenantId,
        Name: m.Name,
        Slug: m.Slug,
        AddressStreet: m.AddressStreet,
        AddressCity: m.AddressCity,
        AddressState: m.AddressState,
        AddressZip: m.AddressZip,
        AddressCountry: m.AddressCountry,
        Latitude: m.Latitude,
        Longitude: m.Longitude,
        PhoneNumber: m.PhoneNumber,
        Email: m.Email,
        Website: m.Website,
        Description: m.Description,
        TimeZoneId: m.TimeZoneId,
        MarinaType: m.MarinaType.ToString(),
        IsListed: m.IsListed,
        IsSetupComplete: m.IsSetupComplete,
        SetupStep: m.SetupStep,
        CreatedAt: m.CreatedAt,
        LogoUrl: logoUrl,
        BannerThumbnailUrl: bannerThumbnailUrl
    );

    internal static DockDto ToDockDto(Dock d) => new(
        Id: d.Id,
        MarinaId: d.MarinaId,
        Name: d.Name,
        Description: d.Description,
        SortOrder: d.SortOrder,
        CreatedAt: d.CreatedAt
    );

    internal static SlipDto ToSlipDto(Slip s) => new(
        Id: s.Id,
        MarinaId: s.MarinaId,
        DockId: s.DockId,
        Name: s.Name,
        SlipType: s.SlipType.ToString(),
        MaxLength: s.MaxLength,
        MaxBeam: s.MaxBeam,
        MaxDraft: s.MaxDraft,
        HasElectric: s.HasElectric,
        Electric: s.Electric.HasValue ? (int)s.Electric.Value : null,
        HasWater: s.HasWater,
        HasPumpOut: s.HasPumpOut,
        IsCovered: s.IsCovered,
        IsIndoor: s.IsIndoor,
        Amenities: s.Amenities.AsReadOnly(),
        Status: s.Status.ToString(),
        ResolvedTransientBaseRate: s.ResolvedTransientBaseRate,
        ResolvedLeaseBaseRate: s.ResolvedLeaseBaseRate,
        PricingPlanId: s.PricingPlanId,
        Notes: s.Notes,
        CreatedAt: s.CreatedAt
    );
}
