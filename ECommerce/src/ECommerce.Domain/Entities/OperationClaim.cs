namespace ECommerce.Domain.Entities
{
    // Bir işlemi yapabilme yetkisi (ör. "Product.Create", "Order.Read", "Admin").
    // User ile çoka-çok ilişkilidir: bir kullanıcının birden çok claim'i, bir claim'in birden çok kullanıcısı olabilir.
    // Ara tablo (UserOperationClaims) EF tarafından yönetilir, bkz. Persistence/Configurations/OperationClaimConfigurations.cs.
    public class OperationClaim
    {
        public int Id { get; set; }

        // Yetki kontrolü bu ad üzerinden METİNSEL yapılır; benzersizdir.
        public string Name { get; set; } = string.Empty;

        public ICollection<User> Users { get; set; } = [];
    }
}
