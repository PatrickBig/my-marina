namespace MyMarina.Infrastructure.Storage;

public sealed record UploadTicket(
    string UploadUrl,
    string Method,
    Dictionary<string, string> RequiredHeaders,
    string Key,
    DateTimeOffset ExpiresAt
);
