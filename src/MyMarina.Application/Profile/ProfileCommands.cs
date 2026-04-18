using MyMarina.Application.Abstractions;

namespace MyMarina.Application.Profile;

public sealed record UpdateProfileCommand(
    string FirstName,
    string LastName,
    string? PhoneNumber);

public sealed record ChangeEmailCommand(
    string NewEmail,
    string CurrentPassword);

public sealed record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword);

public interface IUpdateProfileCommandHandler : ICommandHandler<UpdateProfileCommand>;
public interface IChangeEmailCommandHandler : ICommandHandler<ChangeEmailCommand>;
public interface IChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand>;
