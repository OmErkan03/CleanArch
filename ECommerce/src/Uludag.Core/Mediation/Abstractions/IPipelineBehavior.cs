namespace Uludag.Core.Mediation;

/// <summary>
/// Pipeline'daki bir sonraki adımı (sonraki behavior ya da handler) temsil eder.
/// </summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken = default);

/// <summary>
/// Handler çalışmadan önce/sonra araya giren davranış (logging, validation, transaction, caching vb.).
/// İlk kaydedilen behavior en dışta çalışır.
/// </summary>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
