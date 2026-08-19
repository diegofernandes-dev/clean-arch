using CleanArch.Application.Common.Errors;
using LanguageExt;

namespace CleanArch.Application.Abstractions.Messaging;

public interface IQueryHandler<TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    Task<Either<ApplicationError, TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
}
