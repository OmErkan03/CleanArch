namespace Uludag.Core.Security
{
    // İsteği yapan kullanıcı.
    // Core ve Application katmanları HttpContext'i tanımaz; bu arayüzü WebAPI katmanı (HttpContext.User üzerinden) uygular.
    public interface ICurrentUser
    {
        bool IsAuthenticated { get; }
        Guid? UserId { get; }
        string? Email { get; }

        // İsteğin ait olduğu oturumun Redis'teki anahtarı (JWT'deki "sid"). Logout bu oturumu siler.
        string? SessionKey { get; }

        // Token'daki operation claim adları (ör. "Product.Read"). Oturum açılmamışsa boştur.
        IReadOnlyCollection<string> OperationClaims { get; }
    }
}
