using MediatR;

namespace Reviews.Application.Common.Messaging;

public interface IQuery<TResponse> : IRequest<TResponse> { }
