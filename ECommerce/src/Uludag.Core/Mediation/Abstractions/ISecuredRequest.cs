namespace Uludag.Core.Mediation;

/// <summary>
/// İşaretleyici (marker) arayüz: bu arayüzü uygulayan istekler yalnızca oturum açmış kullanıcılar tarafından çalıştırılabilir.
/// Kontrolü <see cref="Behaviors.AuthenticationBehavior{TRequest, TResponse}"/> yapar.
/// Örnek: <c>public class CreateOrderCommand : IRequest&lt;Guid&gt;, ISecuredRequest</c>
/// </summary>
public interface ISecuredRequest
{
}
