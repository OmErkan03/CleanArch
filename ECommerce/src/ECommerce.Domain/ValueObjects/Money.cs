using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.ValueObjects
{
    // VO => Class değildir
    // "sealed"
    // record =>
    public sealed record Money
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "TRY"; // Default currency is Turkish Lira

        // Para birimi ve miktarını temsil eden bir değer nesnesi.
        // Kurallar yalnızca bu sınıfın içinde tanımlanır.

        public Money(decimal amount, string currency)
        {
            if (amount < 0)
                throw new ArgumentException("Amount cannot be negative.");
            Amount = amount;
            Currency = currency;
        }
    }
}
