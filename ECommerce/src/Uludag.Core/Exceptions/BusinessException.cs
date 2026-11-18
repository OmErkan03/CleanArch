namespace Uludag.Core.Exceptions;

/// <summary>
/// Bir iş kuralı ihlal edildiğinde fırlatılır (ör. aynı isimde ürün var, para birimi uyuşmuyor).
/// Global exception handler bunu 400 Bad Request'e çevirir.
/// </summary>
public class BusinessException : Exception
{
    public BusinessException(string message) : base(message)
    {
    }
}
