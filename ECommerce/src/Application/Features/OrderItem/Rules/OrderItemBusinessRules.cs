using System.Diagnostics.CodeAnalysis;
using Uludag.Core.Exceptions;

namespace ECommerce.Application.Features.OrderItem.Rules
{
    // Sipariş kalemi ile ilgili tüm iş kuralları burada yazılır.
    // Sipariş ve ürünün varlık kontrolleri kendi feature'larındaki Rules sınıflarındadır
    // (OrderBusinessRules, ProductBusinessRules); OrderItem handler'ları onları da kullanır.
    public class OrderItemBusinessRules : BaseBusinessRules
    {
        // [NotNull] => metot hata fırlatmadan dönerse "orderItem artık null değil" bilgisini derleyiciye verir.
        public Task OrderItemShouldExistWhenSelected([NotNull] Domain.Entities.OrderItem? orderItem)
        {
            if (orderItem is null)
                throw new NotFoundException("Sipariş kalemi bulunamadı.");

            return Task.CompletedTask;
        }

        // Farklı para birimleri toplanamaz: siparişe eklenen ürün, siparişle aynı para biriminde olmalıdır.
        public Task ProductCurrencyShouldMatchOrderCurrency(Domain.Entities.Product product, Domain.Entities.Order order)
        {
            if (product.Price.Currency != order.TotalPrice.Currency)
                throw new BusinessException(
                    $"Ürünün para birimi ({product.Price.Currency}) siparişin para birimiyle ({order.TotalPrice.Currency}) aynı olmalıdır.");

            return Task.CompletedTask;
        }
    }
}
