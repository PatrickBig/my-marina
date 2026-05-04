namespace MyMarina.Application.Identity;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    bool MarketingOptIn,
    bool TermsAccepted
);

public sealed record LoginCommand(
    string Email,
    string Password,
    string? IpAddress
);

public sealed record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress
);

public sealed record ForgotPasswordCommand(string Email);

public sealed record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword
);

public sealed record ConfirmEmailCommand(
    string UserId,
    string Token
);

public sealed record ResendConfirmationCommand(string Email);

public sealed record LogoutCommand(string RefreshToken);
