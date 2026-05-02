namespace MyMarina.Application.Identity;

public sealed record ExternalLoginCommand(
    string Provider,
    string ProviderKey,
    string? Email,
    string? FirstName,
    string? LastName,
    string? IpAddress
);

public sealed record LinkExternalProviderCommand(
    Guid UserId,
    string Provider,
    string ProviderKey
);

public sealed record UnlinkExternalProviderCommand(
    Guid UserId,
    string Provider
);

public sealed record GetLinkedProvidersQuery(Guid UserId);

public sealed record LinkedProviderDto(string Provider);

public sealed class ExternalEmailCollisionException()
    : Exception("An account with this email already exists. Sign in with your existing method and link this provider from your profile.");
