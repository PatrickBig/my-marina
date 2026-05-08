namespace MyMarina.Application.Demo;

public sealed record DemoSessionQuery;

public sealed record DemoSessionResponse(string AccessToken, string ExpiresAt);
