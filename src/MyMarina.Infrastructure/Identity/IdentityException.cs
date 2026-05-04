using Microsoft.AspNetCore.Identity;

namespace MyMarina.Infrastructure.Identity;

public class IdentityException(IEnumerable<IdentityError> errors) : Exception(
    string.Join("; ", errors.Select(e => e.Description)))
{
    public IReadOnlyList<IdentityError> Errors { get; } = errors.ToList();
}
