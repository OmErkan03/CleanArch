using System.Collections.Concurrent;
using Uludag.Core.Mediation.Internal;

namespace Uludag.Core.Mediation;

/// <summary>
/// Dış paket gerektirmeyen mediator implementasyonu.
/// Handler'ları ve behavior'ları yalnızca <see cref="IServiceProvider"/> üzerinden çözer;
/// bu sayede Microsoft DI dahil IServiceProvider sunan her container ile çalışır.
/// </summary>
public class Mediator : IMediator
{
    private static readonly ConcurrentDictionary<(Type Request, Type Response), object> RequestWrappers = new();
    private static readonly ConcurrentDictionary<Type, NotificationHandlerWrapper> NotificationWrappers = new();

    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapper = (RequestHandlerWrapper<TResponse>)RequestWrappers.GetOrAdd(
            (request.GetType(), typeof(TResponse)),
            static key => Activator.CreateInstance(
                typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(key.Request, key.Response))!);

        return wrapper.Handle(request, _serviceProvider, cancellationToken);
    }

    public Task Send(IRequest request, CancellationToken cancellationToken = default)
        => Send<Unit>(request, cancellationToken);

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        // Derleme zamanı tipi yerine gerçek tip kullanılır; böylece INotification tipli
        // bir değişkenle yayınlanan bildirim de doğru handler'lara ulaşır.
        var wrapper = NotificationWrappers.GetOrAdd(
            notification.GetType(),
            static type => (NotificationHandlerWrapper)Activator.CreateInstance(
                typeof(NotificationHandlerWrapperImpl<>).MakeGenericType(type))!);

        var handlers = wrapper.GetHandlers(notification, _serviceProvider);
        return PublishCore(handlers, notification, cancellationToken);
    }

    /// <summary>
    /// Bildirimi handler'lara dağıtma stratejisi. Varsayılan: sırayla, birbiri ardına.
    /// Paralel yayın vb. farklı bir strateji için türetilmiş sınıfta override edilebilir.
    /// </summary>
    protected virtual async Task PublishCore(
        IReadOnlyList<NotificationHandlerExecutor> handlers,
        INotification notification,
        CancellationToken cancellationToken)
    {
        foreach (var handler in handlers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await handler.HandlerCallback(cancellationToken).ConfigureAwait(false);
        }
    }
}
