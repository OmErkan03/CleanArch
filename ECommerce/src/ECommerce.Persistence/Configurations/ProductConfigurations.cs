using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations
{
    public class ProductConfigurations : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            // NOT: Seed data burada HasData ile verilmez; Price bir ComplexProperty olduğu için
            // EF Core HasData ile seed edilmesini desteklemiyor (efcore#31254).
            // Seed işlemi DbContext options'taki UseSeeding/UseAsyncSeeding ile yapılır (bkz. Seeds/ProductSeed.cs).
            builder.ToTable("Products");

            builder.HasKey(p => p.Id);

            // HasMaxLength veritabanından bağımsızdır: PostgreSQL'de varchar(100), SQL Server'da nvarchar(100) olur.
            // ("NVARCHAR" yalnızca SQL Server'da vardır, PostgreSQL'de hata verir.)
            builder.Property(p => p.Name)
                .HasMaxLength(100)
                .IsRequired(false);

            // Money bir Value Object'tir: kendi tablosu/Id'si yoktur, Products tablosunda kolon olarak saklanır.
            builder.ComplexProperty(p => p.Price, price =>
            {
                price.Property(m => m.Amount)
                    .HasColumnName("PriceAmount")
                    .HasPrecision(18, 2);

                price.Property(m => m.Currency)
                    .HasColumnName("PriceCurrency")
                    .HasMaxLength(3);
            });
        }
    }
}
