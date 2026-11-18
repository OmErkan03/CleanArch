using ECommerce.Domain.Entities;
using ECommerce.Persistence.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Configurations
{
    public class OperationClaimConfigurations : IEntityTypeConfiguration<OperationClaim>
    {
        public void Configure(EntityTypeBuilder<OperationClaim> builder)
        {
            builder.ToTable("OperationClaims");

            builder.HasKey(oc => oc.Id);

            // Id'ler kodda sabit verilir (bkz. OperationClaimSeed). Veritabanı üretseydi (identity), seed ile verilen
            // 1..N Id'leri sequence'ı ilerletmeyeceği için sonraki ilk INSERT "duplicate key" hatası verirdi.
            builder.Property(oc => oc.Id).ValueGeneratedNever();

            builder.Property(oc => oc.Name)
                .HasMaxLength(100)
                .IsRequired();

            // Yetki kontrolü ada göre yapıldığı için aynı ad iki kez tanımlanamaz.
            builder.HasIndex(oc => oc.Name).IsUnique();

            // User <-> OperationClaim çoka-çok ilişkisi.
            // Ara tabloyu ayrı bir entity olarak yazmak yerine EF'e bırakırız; yalnızca tablo ve kolon adlarını belirleriz.
            // Kullanıcı ya da claim silinince ara tablodaki satırları da silinir (cascade).
            builder.HasMany(oc => oc.Users)
                .WithMany(u => u.OperationClaims)
                .UsingEntity(
                    "UserOperationClaims",
                    right => right.HasOne(typeof(User)).WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade),
                    left => left.HasOne(typeof(OperationClaim)).WithMany().HasForeignKey("OperationClaimId").OnDelete(DeleteBehavior.Cascade),
                    join => join.HasKey("UserId", "OperationClaimId"));

            // OperationClaim basit bir entity olduğu için HasData ile seed edilebilir (migration'a INSERT olarak yazılır).
            builder.HasData(OperationClaimSeed.GetOperationClaims());
        }
    }
}
