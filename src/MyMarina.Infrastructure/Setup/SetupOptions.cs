namespace MyMarina.Infrastructure.Setup;

/// <summary>
/// Configuration for the /setup bootstrapping pass. Bind from the "Setup" section.
/// In Kubernetes, inject values via Secrets as environment variables using
/// double-underscore notation: Setup__PlatformOperator__Email, Setup__PlatformOperator__Password, etc.
/// </summary>
public sealed class SetupOptions
{
    public const string Section = "Setup";

    /// <summary>The root platform-operator account. If omitted, that step is skipped.</summary>
    public PlatformOperatorOptions? PlatformOperator { get; set; }

    /// <summary>
    /// First marina tenant + owner. If omitted, that step is skipped.
    /// Useful for single-tenant deployments that want a ready-to-use account out of the box.
    /// </summary>
    public InitialMarinaOptions? InitialMarina { get; set; }
}

public sealed class PlatformOperatorOptions
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = "Platform";
    public string LastName { get; set; } = "Admin";
    public string Password { get; set; } = string.Empty;
}

public sealed class InitialMarinaOptions
{
    public string TenantName { get; set; } = string.Empty;
    public string TenantSlug { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string OwnerFirstName { get; set; } = "Marina";
    public string OwnerLastName { get; set; } = "Owner";
    public string OwnerPassword { get; set; } = string.Empty;
}
