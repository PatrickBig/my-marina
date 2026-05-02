namespace MyMarina.Infrastructure.Email.Templates;

internal static class EmailTemplates
{
    public static string EmailConfirmation(string toEmail, string userId, string token) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">
          <h2 style="color: #1a56db;">Confirm your email address</h2>
          <p>Hi {HtmlEncode(toEmail)},</p>
          <p>Please confirm your email address. Your confirmation code is:</p>
          <p style="font-family: monospace; font-size: 16px; background: #f3f4f6; padding: 12px; border-radius: 6px;">{HtmlEncode(token)}</p>
          <p style="color: #666; font-size: 13px;">User ID: {HtmlEncode(userId)}</p>
          <p style="color: #666; font-size: 13px;">If you did not create a MyMarina account, you can ignore this email.</p>
          <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 24px 0;" />
          <p style="color: #9ca3af; font-size: 12px;">MyMarina &mdash; Marina Marketplace</p>
        </body>
        </html>
        """;

    public static string PasswordReset(string toEmail, string userId, string token) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">
          <h2 style="color: #1a56db;">Reset your password</h2>
          <p>Hi {HtmlEncode(toEmail)},</p>
          <p>We received a request to reset your password. Your reset token is:</p>
          <p style="font-family: monospace; font-size: 16px; background: #f3f4f6; padding: 12px; border-radius: 6px;">{HtmlEncode(token)}</p>
          <p style="color: #666; font-size: 13px;">User ID: {HtmlEncode(userId)}</p>
          <p style="color: #666; font-size: 13px;">This token expires in 1 hour. If you did not request a password reset, you can ignore this email.</p>
          <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 24px 0;" />
          <p style="color: #9ca3af; font-size: 12px;">MyMarina &mdash; Marina Marketplace</p>
        </body>
        </html>
        """;

    public static string MembershipInvite(string toEmail, string marinaName, string invitedByName, Guid membershipId) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">
          <h2 style="color: #1a56db;">You've been invited to {HtmlEncode(marinaName)}</h2>
          <p>Hi {HtmlEncode(toEmail)},</p>
          <p>{HtmlEncode(invitedByName)} has invited you to join <strong>{HtmlEncode(marinaName)}</strong> on MyMarina as a staff member.</p>
          <p>Your invitation ID: <code>{membershipId}</code></p>
          <p>Sign in to MyMarina and accept the invitation from your profile to get started.</p>
          <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 24px 0;" />
          <p style="color: #9ca3af; font-size: 12px;">MyMarina &mdash; Marina Marketplace</p>
        </body>
        </html>
        """;

    public static string GhostVesselClaim(string toEmail, string marinaName, string vesselName, Guid vesselId) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">
          <h2 style="color: #1a56db;">Your boat has been added to MyMarina</h2>
          <p>Hi {HtmlEncode(toEmail)},</p>
          <p><strong>{HtmlEncode(marinaName)}</strong> has added your boat <strong>{HtmlEncode(vesselName)}</strong> to their records on MyMarina.</p>
          <p>Sign in (or create an account) at MyMarina to confirm the boat details and take ownership of your record. Once you accept, you'll be able to manage your boat, view marina communications, and use it for future bookings.</p>
          <p style="color: #666; font-size: 13px;">Boat ID: {vesselId}</p>
          <p style="color: #666; font-size: 13px;">If you don't recognize this marina or didn't expect this email, you can safely ignore it.</p>
          <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 24px 0;" />
          <p style="color: #9ca3af; font-size: 12px;">MyMarina &mdash; Marina Marketplace</p>
        </body>
        </html>
        """;

    public static string BillingAccountInvite(string toEmail, string marinaName, string invitedByName, Guid memberId) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">
          <h2 style="color: #1a56db;">You've been added to a billing account at {HtmlEncode(marinaName)}</h2>
          <p>Hi {HtmlEncode(toEmail)},</p>
          <p>{HtmlEncode(invitedByName)} at <strong>{HtmlEncode(marinaName)}</strong> has added you to a billing account on MyMarina.</p>
          <p>Sign in to MyMarina to view your account, invoices, and billing history.</p>
          <p style="color: #666; font-size: 13px;">Member ID: {memberId}</p>
          <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 24px 0;" />
          <p style="color: #9ca3af; font-size: 12px;">MyMarina &mdash; Marina Marketplace</p>
        </body>
        </html>
        """;

    private static string HtmlEncode(string value) =>
        System.Net.WebUtility.HtmlEncode(value);
}
