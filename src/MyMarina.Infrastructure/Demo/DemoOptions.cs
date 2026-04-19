namespace MyMarina.Infrastructure.Demo;

public sealed class DemoOptions
{
    public const string Section = "Demo";

    public int TtlMinutes { get; set; } = 60;
    public string DefaultTier { get; set; } = "Pro";
}
