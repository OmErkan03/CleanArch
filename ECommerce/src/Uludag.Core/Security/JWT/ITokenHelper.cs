namespace Uludag.Core.Security.JWT
{
    // Token yalnızca Redis'teki oturumun anahtarını taşır (bkz. ISessionStore).
    // Kullanıcı bilgileri (email, yetkiler) token'da değil, Redis'teki oturum kaydında durur.
    // Application katmanı bu arayüze bağımlıdır, JWT'nin nasıl üretildiğini bilmez.
    public interface ITokenHelper
    {
        // expiration: oturumun Redis'teki bitiş zamanıyla aynı olmalıdır (UTC).
        AccessToken CreateToken(string sessionKey, DateTime expiration);
    }
}
