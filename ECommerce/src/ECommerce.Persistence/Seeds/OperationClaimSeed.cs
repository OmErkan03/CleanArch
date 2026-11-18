using ECommerce.Application.Features.Order.Constants;
using ECommerce.Application.Features.OrderItem.Constants;
using ECommerce.Application.Features.Product.Constants;
using ECommerce.Domain.Entities;
using Uludag.Core.Security.Constants;

namespace ECommerce.Persistence.Seeds
{
    // Sistemde tanımlı tüm operation claim'ler.
    // OperationClaimConfigurations içinde HasData ile kullanılır => "dotnet ef migrations add" bunları migration'a yazar.
    //
    // Id'ler SABİTTİR ve DEĞİŞTİRİLMEMELİDİR: HasData kayıtları Id ile eşleştirir; Id değişirse EF silip yeniden ekler
    // ve kullanıcılara verilmiş yetkiler (UserOperationClaims) kaybolur.
    // Yeni entity eklerken listenin SONUNA, kullanılmamış yeni Id'lerle ekleyin ve yeni bir migration oluşturun.
    internal static class OperationClaimSeed
    {
        public static OperationClaim[] GetOperationClaims() =>
        [
            new() { Id = 1, Name = GeneralOperationClaims.Admin },

            new() { Id = 2, Name = ProductOperationClaims.Admin },
            new() { Id = 3, Name = ProductOperationClaims.Read },
            new() { Id = 4, Name = ProductOperationClaims.Write },
            new() { Id = 5, Name = ProductOperationClaims.Create },
            new() { Id = 6, Name = ProductOperationClaims.Update },
            new() { Id = 7, Name = ProductOperationClaims.Delete },

            new() { Id = 8, Name = OrderOperationClaims.Admin },
            new() { Id = 9, Name = OrderOperationClaims.Read },
            new() { Id = 10, Name = OrderOperationClaims.Write },
            new() { Id = 11, Name = OrderOperationClaims.Create },
            new() { Id = 12, Name = OrderOperationClaims.Update },
            new() { Id = 13, Name = OrderOperationClaims.Delete },

            new() { Id = 14, Name = OrderItemOperationClaims.Admin },
            new() { Id = 15, Name = OrderItemOperationClaims.Read },
            new() { Id = 16, Name = OrderItemOperationClaims.Write },
            new() { Id = 17, Name = OrderItemOperationClaims.Create },
            new() { Id = 18, Name = OrderItemOperationClaims.Update },
            new() { Id = 19, Name = OrderItemOperationClaims.Delete },
        ];
    }
}
