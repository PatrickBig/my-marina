using MyMarina.Domain.Enums;

namespace MyMarina.Domain.Entities;

public class Marina
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }

    // Address — stored as flat columns; optional until marina goes live
    public string? AddressStreet { get; set; }
    public string? AddressCity { get; set; }
    public string? AddressState { get; set; }
    public string? AddressZip { get; set; }
    public string? AddressCountry { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Description { get; set; }
    public string TimeZoneId { get; set; } = "UTC";
    public MarinaType MarinaType { get; set; } = MarinaType.Commercial;
    public bool IsListed { get; set; }

    // JSON blob: MarinaOnboardingConfig — configures which post-approval steps run automatically
    public string? OnboardingConfig { get; set; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
