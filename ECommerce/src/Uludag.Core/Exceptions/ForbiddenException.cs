namespace Uludag.Core.Exceptions;

/// <summary>
/// Kullanıcı oturum açmış ancak isteğin gerektirdiği operation claim'lerden hiçbirine sahip değilse fırlatılır.
/// Global exception handler bunu 403 Forbidden'a çevirir.
/// (401 = "kim olduğunu bilmiyorum", 403 = "kim olduğunu biliyorum ama bu işleme yetkin yok")
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "Bu işlem için yetkiniz yok.") : base(message)
    {
    }
}
