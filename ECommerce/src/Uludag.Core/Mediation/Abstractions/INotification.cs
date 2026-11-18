namespace Uludag.Core.Mediation;

/// <summary>
/// Birden fazla handler tarafından dinlenebilen bildirim (event). Geriye değer döndürmez.
/// </summary>
public interface INotification
{
}

/// <summary>
/// <typeparamref name="TNotification"/> bildirimini dinleyen handler. Bir bildirimin sıfır veya daha fazla handler'ı olabilir.
/// </summary>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
