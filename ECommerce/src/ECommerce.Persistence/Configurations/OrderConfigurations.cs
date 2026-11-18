using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations
{
    public class OrderConfigurations : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");

            builder.HasKey(o => o.Id);

            builder.Property(o => o.OrderDate)
                .IsRequired();

            // Money bir Value Object: Orders tablosunda kolon olarak saklanır (bkz. ProductConfigurations).
            builder.ComplexProperty(o => o.TotalPrice, price =>
            {
                price.Property(m => m.Amount)
                    .HasColumnName("TotalPriceAmount")
                    .HasPrecision(18, 2);

                price.Property(m => m.Currency)
                    .HasColumnName("TotalPriceCurrency")
                    .HasMaxLength(3);
            });

            // İlişki (Order 1 - N OrderItem) OrderItemConfigurations'ta tanımlanır.
        }
    }
}
