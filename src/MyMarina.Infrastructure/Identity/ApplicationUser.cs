using Microsoft.AspNetCore.Identity;

namespace MyMarina.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public bool MarketingOptIn { get; set; }
    public DateTimeOffset? TermsAcceptedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
