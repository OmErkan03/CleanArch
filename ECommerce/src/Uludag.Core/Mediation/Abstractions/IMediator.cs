namespace Uludag.Core.Mediation;

/// <summary>
/// İsteği (Command / Query) ilgili tek handler'a iletir.
/// </summary>
public interface ISender
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    Task Send(IRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Bildirimi (event) onu dinleyen tüm handler'lara yayınlar.
/// </summary>
public interface IPublisher
{
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}

/// <summary>
/// Hem istek gönderebilen hem bildirim yayınlayabilen mediator.
/// </summary>
public interface IMediator : ISender, IPublisher
{
}
