using CleanArch.Application.Common.Errors;
using LanguageExt;

namespace CleanArch.Application.Abstractions.Messaging;

public interface ICommandHandler<TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    Task<Either<ApplicationError, TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
}
