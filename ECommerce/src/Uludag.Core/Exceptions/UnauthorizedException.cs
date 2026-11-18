namespace Uludag.Core.Exceptions;

/// <summary>
/// Oturum açılmadan (geçerli token olmadan) korunan bir istek çalıştırılmak istendiğinde fırlatılır.
/// Global exception handler bunu 401 Unauthorized'a çevirir.
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "Bu işlem için giriş yapmalısınız.") : base(message)
    {
    }
}
