namespace MyMarina.Application.Abstractions;

public sealed record SendEmailMessage(
    string ToEmail,
    string ToName,
    string Subject,
    string HtmlBody);
