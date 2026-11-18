namespace Uludag.Core.Security.JWT
{
    // Core katmanı Domain'i (User entity'sini) tanımaz; bu yüzden token üretmek için gereken bilgiler ayrı ayrı verilir.
    // Application katmanı bu arayüze bağımlıdır, JWT'nin nasıl üretildiğini bilmez.
    public interface ITokenHelper
    {
        AccessToken CreateToken(Guid userId, string email, IEnumerable<string> operationClaims);
    }
}
