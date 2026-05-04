namespace MyMarina.Application.Leases;

/// <summary>
/// Stored as JSON in Marina.OnboardingConfig.
/// Controls which steps run automatically when a SlipLeaseInquiry is approved.
/// </summary>
public sealed class MarinaOnboardingConfig
{
    public bool CreateWelcomeWorkOrder { get; set; }
    public string WelcomeWorkOrderTitle { get; set; } = "New Slip Assignment — Setup";
    public string WelcomeWorkOrderDescription { get; set; } =
        "Prepare slip for new tenant: check electrical/water hookups, install nameplate, inspect dock hardware.";

    public bool SendWelcomeEmail { get; set; }
    public string? WelcomeEmailSubject { get; set; }
    public string? WelcomeEmailBodyTemplate { get; set; }   // future: Handlebars/Liquid template
}
