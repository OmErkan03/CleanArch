namespace Uludag.Core.Mediation.Internal;

/// <summary>
/// Çalışma zamanında bilinen istek tipini, derleme zamanında generic olan handler'a bağlar.
/// Her istek tipi için bir kez oluşturulup önbelleğe alınır (reflection maliyeti tek seferlik).
/// </summary>
internal abstract class RequestHandlerWrapper<TResponse>
{
    public abstract Task<TResponse> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

internal sealed class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse>
{
    public override Task<TResponse> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;

        var handler = serviceProvider.GetService(typeof(IRequestHandler<TRequest, TResponse>)) as IRequestHandler<TRequest, TResponse>
            ?? throw new InvalidOperationException(
                $"'{typeof(TRequest).FullName}' isteği için bir handler bulunamadı. " +
                $"'IRequestHandler<{typeof(TRequest).Name}, {typeof(TResponse).Name}>' uygulayan sınıfın DI container'a kaydedildiğinden emin olun.");

        RequestHandlerDelegate<TResponse> pipeline = ct => handler.Handle(typedRequest, ct);

        var behaviors = ServiceProviderExtensions.GetServices<IPipelineBehavior<TRequest, TResponse>>(serviceProvider);

        // Sondan başa sararak ilk kaydedilen behavior'ın en dışta çalışmasını sağlarız.
        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var next = pipeline;
            pipeline = ct => behavior.Handle(typedRequest, next, ct);
        }

        return pipeline(cancellationToken);
    }
}
