namespace MyMarina.Infrastructure.Setup;

public class SetupOptions
{
    public PlatformOperatorSetupOptions PlatformOperator { get; set; } = new();
    public InitialMarinaSetupOptions InitialMarina { get; set; } = new();
}

public class PlatformOperatorSetupOptions
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class InitialMarinaSetupOptions
{
    public string TenantName { get; set; } = "";
    public string TenantSlug { get; set; } = "";
    public string OwnerEmail { get; set; } = "";
    public string OwnerPassword { get; set; } = "";
}
