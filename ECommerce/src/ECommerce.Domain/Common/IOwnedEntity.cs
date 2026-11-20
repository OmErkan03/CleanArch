namespace ECommerce.Domain.Common
{
    // Bir kullanıcıya ait olan (sahibi olan) entity'leri işaretler.
    //
    // IDOR (Insecure Direct Object Reference) koruması bu arayüz üzerinden otomatik uygulanır (bkz. UludagDbContext.Ownership.cs):
    //   - Okuma: global query filter => kullanıcı yalnızca kendi kayıtlarını görür; başkasının kaydının Id'si
    //     tahmin edilse/bilinse bile sorgu "bulunamadı" döner (404). Kaydın var olduğu bile sızdırılmaz.
    //   - Yazma: SaveChanges => başkasına ait kayıt eklenemez, güncellenemez, silinemez ve sahibi değiştirilemez (403).
    // "{EntityAdı}.Admin" veya genel "Admin" claim'ine sahip kullanıcılar tüm kayıtlara erişebilir.
    //
    // UserId'nin setter'ı arayüzde yoktur: sahiplik istemciden değil, oturumdaki kullanıcıdan belirlenir.
    public interface IOwnedEntity
    {
        Guid UserId { get; }
    }
}
