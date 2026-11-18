namespace Uludag.Core.Mediation.Internal;

/// <summary>
/// Bildirimi çalışma zamanındaki gerçek tipine göre handler'lara bağlar.
/// </summary>
internal abstract class NotificationHandlerWrapper
{
    public abstract IReadOnlyList<NotificationHandlerExecutor> GetHandlers(object notification, IServiceProvider serviceProvider);
}

internal sealed class NotificationHandlerWrapperImpl<TNotification> : NotificationHandlerWrapper
    where TNotification : INotification
{
    public override IReadOnlyList<NotificationHandlerExecutor> GetHandlers(object notification, IServiceProvider serviceProvider)
    {
        var typedNotification = (TNotification)notification;
        var handlers = ServiceProviderExtensions.GetServices<INotificationHandler<TNotification>>(serviceProvider);

        var executors = new NotificationHandlerExecutor[handlers.Length];
        for (var i = 0; i < handlers.Length; i++)
        {
            var handler = handlers[i];
            executors[i] = new NotificationHandlerExecutor(handler, ct => handler.Handle(typedNotification, ct));
        }

        return executors;
    }
}
