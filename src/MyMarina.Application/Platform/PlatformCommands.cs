using MyMarina.Application.Abstractions;

namespace MyMarina.Application.Platform;

public sealed record ResetUserPasswordCommand(Guid UserId, string NewPassword);
public sealed record DeactivateUserCommand(Guid UserId);
public sealed record ReactivateUserCommand(Guid UserId);

public interface IResetUserPasswordCommandHandler : ICommandHandler<ResetUserPasswordCommand>;
public interface IDeactivateUserCommandHandler : ICommandHandler<DeactivateUserCommand>;
public interface IReactivateUserCommandHandler : ICommandHandler<ReactivateUserCommand>;
