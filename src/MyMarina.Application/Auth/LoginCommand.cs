using MyMarina.Application.Abstractions;

namespace MyMarina.Application.Auth;

public sealed record LoginCommand(string Email, string Password);

public sealed record LoginResult(
    string? Token,
    DateTimeOffset ExpiresAt,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string? Role,
    Guid? TenantId,
    Guid? MarinaId,
    List<AvailableContext> AvailableContexts = null!);
    // If multiple contexts exist, Token is null and client must call ChooseContext endpoint

public interface ILoginCommandHandler : ICommandHandler<LoginCommand, LoginResult>;
