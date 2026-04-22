using MailKit.Security;

namespace MyMarina.Infrastructure.Email;

public sealed class EmailOptions
{
    public string Provider { get; init; } = "null";
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 587;

    /// <summary>Maps to MailKit's SecureSocketOptions: None, Auto, SslOnConnect, StartTls.</summary>
    public string SecureSocket { get; init; } = "StartTls";

    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = "MyMarina";
    public bool RequireConfirmedEmail { get; init; } = false;

    /// <summary>Base URL of the web app, used for building confirmation links.</summary>
    public string AppBaseUrl { get; init; } = "http://localhost:5173";

    /// <summary>Email domains that will never receive real emails (e.g. demo tenants).</summary>
    public List<string> ExcludedDomains { get; init; } = [];

    public SecureSocketOptions GetSecureSocketOptions() => SecureSocket switch
    {
        "SslOnConnect" => SecureSocketOptions.SslOnConnect,
        "StartTls"     => SecureSocketOptions.StartTls,
        "None"         => SecureSocketOptions.None,
        _              => SecureSocketOptions.Auto,
    };
}
