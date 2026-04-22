namespace MyMarina.Infrastructure.Email.Templates;

internal static class EmailTemplates
{
    public static string CustomerInvite(string customerName, string marinaName, string temporaryPassword, string confirmationLink) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">
          <h2 style="color: #1a56db;">Welcome to {HtmlEncode(marinaName)}!</h2>
          <p>Hi {HtmlEncode(customerName)},</p>
          <p>Your marina portal account has been created. Use the details below to log in for the first time.</p>
          <table style="background: #f3f4f6; border-radius: 6px; padding: 16px; margin: 16px 0; width: 100%; border-collapse: collapse;">
            <tr><td style="padding: 4px 8px; font-weight: bold;">Email</td><td style="padding: 4px 8px; font-family: monospace;">{HtmlEncode(customerName)}</td></tr>
            <tr><td style="padding: 4px 8px; font-weight: bold;">Temporary&nbsp;Password</td><td style="padding: 4px 8px; font-family: monospace;">{HtmlEncode(temporaryPassword)}</td></tr>
          </table>
          <p>You must confirm your email address before logging in. Click the button below:</p>
          <p style="text-align: center; margin: 24px 0;">
            <a href="{confirmationLink}" style="background: #1a56db; color: white; padding: 12px 24px; border-radius: 6px; text-decoration: none; font-weight: bold;">Confirm Email &amp; Activate Account</a>
          </p>
          <p style="color: #666; font-size: 13px;">This link expires in 24 hours. If you did not expect this email, please ignore it.</p>
          <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 24px 0;" />
          <p style="color: #9ca3af; font-size: 12px;">MyMarina &mdash; Marina Management Platform</p>
        </body>
        </html>
        """;

    public static string StaffInvite(string staffName, string marinaName, string role, string temporaryPassword, string confirmationLink) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">
          <h2 style="color: #1a56db;">You've been invited to {HtmlEncode(marinaName)}</h2>
          <p>Hi {HtmlEncode(staffName)},</p>
          <p>You have been added to <strong>{HtmlEncode(marinaName)}</strong> as <strong>{HtmlEncode(role)}</strong>. Use the details below to log in for the first time.</p>
          <table style="background: #f3f4f6; border-radius: 6px; padding: 16px; margin: 16px 0; width: 100%; border-collapse: collapse;">
            <tr><td style="padding: 4px 8px; font-weight: bold;">Temporary&nbsp;Password</td><td style="padding: 4px 8px; font-family: monospace;">{HtmlEncode(temporaryPassword)}</td></tr>
          </table>
          <p>Confirm your email address to activate your account:</p>
          <p style="text-align: center; margin: 24px 0;">
            <a href="{confirmationLink}" style="background: #1a56db; color: white; padding: 12px 24px; border-radius: 6px; text-decoration: none; font-weight: bold;">Confirm Email &amp; Activate Account</a>
          </p>
          <p style="color: #666; font-size: 13px;">This link expires in 7 days. If you did not expect this email, please ignore it.</p>
          <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 24px 0;" />
          <p style="color: #9ca3af; font-size: 12px;">MyMarina &mdash; Marina Management Platform</p>
        </body>
        </html>
        """;

    public static string EmailConfirmation(string recipientName, string confirmationLink) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">
          <h2 style="color: #1a56db;">Confirm your email address</h2>
          <p>Hi {HtmlEncode(recipientName)},</p>
          <p>Please confirm your email address by clicking the button below:</p>
          <p style="text-align: center; margin: 24px 0;">
            <a href="{confirmationLink}" style="background: #1a56db; color: white; padding: 12px 24px; border-radius: 6px; text-decoration: none; font-weight: bold;">Confirm Email Address</a>
          </p>
          <p style="color: #666; font-size: 13px;">If you did not request this, please ignore it.</p>
          <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 24px 0;" />
          <p style="color: #9ca3af; font-size: 12px;">MyMarina &mdash; Marina Management Platform</p>
        </body>
        </html>
        """;

    private static string HtmlEncode(string value) =>
        System.Net.WebUtility.HtmlEncode(value);
}
