namespace Uludag.Core.Mediation;

/// <summary>
/// İşaretleyici (marker) arayüz: bu arayüzü uygulayan istekler oturum açmadan (token olmadan) çalıştırılabilir.
/// <see cref="Behaviors.AuthenticationBehavior{TRequest, TResponse}"/> varsayılan olarak TÜM istekleri korur;
/// bu işaret yalnızca herkese açık olması gereken istekler (Login, Register...) için kullanılır.
/// Örnek: <c>public class LoginCommand : IRequest&lt;AccessToken&gt;, IAllowAnonymousRequest</c>
/// </summary>
public interface IAllowAnonymousRequest
{
}
