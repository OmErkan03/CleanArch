using System.Diagnostics.CodeAnalysis;
using Uludag.Core.Exceptions;

namespace ECommerce.Application.Features.Order.Rules
{
    // Sipariş nesnesi ile ilgili tüm iş kuralları burada yazılır.
    public class OrderBusinessRules : BaseBusinessRules
    {
        // [NotNull] => metot hata fırlatmadan dönerse "order artık null değil" bilgisini derleyiciye verir.
        public Task OrderShouldExistWhenSelected([NotNull] Domain.Entities.Order? order)
        {
            if (order is null)
                throw new NotFoundException("Sipariş bulunamadı.");

            return Task.CompletedTask;
        }
    }
}
