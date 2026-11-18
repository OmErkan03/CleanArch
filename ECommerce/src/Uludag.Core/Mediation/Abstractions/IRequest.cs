namespace Uludag.Core.Mediation;

/// <summary>
/// Geriye <typeparamref name="TResponse"/> döndüren bir istek (Command / Query).
/// </summary>
public interface IRequest<out TResponse>
{
}

/// <summary>
/// Geriye değer döndürmeyen bir istek.
/// </summary>
public interface IRequest : IRequest<Unit>
{
}
