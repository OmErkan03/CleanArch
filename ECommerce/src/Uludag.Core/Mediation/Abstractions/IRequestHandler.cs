namespace Uludag.Core.Mediation;

/// <summary>
/// <typeparamref name="TRequest"/> isteğini işleyip <typeparamref name="TResponse"/> döndüren handler.
/// Her istek tipi için yalnızca bir handler bulunmalıdır.
/// </summary>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Geriye değer döndürmeyen <typeparamref name="TRequest"/> isteğini işleyen handler.
/// </summary>
public interface IRequestHandler<in TRequest> : IRequestHandler<TRequest, Unit>
    where TRequest : IRequest
{
    new Task Handle(TRequest request, CancellationToken cancellationToken);

    // Mediator her zaman IRequestHandler<TRequest, TResponse> ile çalışır;
    // void handler'lar bu köprü sayesinde otomatik olarak Unit döndürür.
    async Task<Unit> IRequestHandler<TRequest, Unit>.Handle(TRequest request, CancellationToken cancellationToken)
    {
        await Handle(request, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
