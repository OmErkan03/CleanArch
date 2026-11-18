namespace Uludag.Core.Exceptions;

/// <summary>
/// İstenen kayıt bulunamadığında fırlatılır.
/// Global exception handler bunu 404 Not Found'a çevirir.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
