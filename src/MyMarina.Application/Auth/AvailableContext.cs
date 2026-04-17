namespace MyMarina.Application.Auth;

public sealed record AvailableContext(
    string DisplayName,
    string? Role,
    Guid TenantId,
    Guid? MarinaId,
    Guid? CustomerAccountId);

public sealed record ChooseContextCommand(Guid UserId, AvailableContext Context);

public sealed record ContextToken(
    string Token,
    DateTimeOffset ExpiresAt);
