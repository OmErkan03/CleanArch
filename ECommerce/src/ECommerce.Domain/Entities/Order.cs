using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; set; }
        public DateTime OrderDate { get; set; }
        public Money TotalPrice { get; set; } = default!;

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
