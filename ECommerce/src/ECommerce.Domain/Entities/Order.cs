using ECommerce.Domain.Common;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities
{
    // IOwnedEntity => sipariş yalnızca sahibi (veya Order.Admin / Admin) tarafından görülebilir ve değiştirilebilir.
    public class Order : IOwnedEntity
    {
        public Guid Id { get; set; }
        public DateTime OrderDate { get; set; }
        public Money TotalPrice { get; set; } = default!;

        // Siparişin sahibi (N Order => 1 User). İstemciden alınmaz; siparişi oluşturan oturumdaki kullanıcıdır.
        public Guid UserId { get; set; }
        public virtual User User { get; set; } = default!;

        // Navigation property: siparişin kalemleri (1 Order => N OrderItem)
        public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

        // Siparişin toplam tutarı, kalemlerin toplamından hesaplanır; istemciden alınmaz.
        // Kalem eklendiğinde / güncellendiğinde / silindiğinde çağrılmalıdır.
        public void RecalculateTotalPrice()
        {
            var total = Items.Sum(i => i.TotalPrice.Amount);
            // Money bir Value Object: alanları tek tek değiştirilmez, yenisi oluşturulur.
            TotalPrice = new Money(total, TotalPrice.Currency);
        }
    }
}
