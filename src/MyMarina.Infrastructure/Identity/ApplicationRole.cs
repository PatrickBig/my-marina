using Microsoft.AspNetCore.Identity;

namespace MyMarina.Infrastructure.Identity;

/// <summary>
/// ASP.NET Core Identity role. Extended to use Guid PK to match UUID v7 strategy.
/// </summary>
public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}
