using System;
using System.Collections.Generic;
using System.Text;

namespace Uludag.Core.Mediation.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull, IRequest<TResponse>
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            Console.WriteLine("1");
            var response = await next(cancellationToken);
            Console.WriteLine("2");
            return response;
        }
    }
}
