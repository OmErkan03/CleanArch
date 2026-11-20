using ECommerce.Domain.ValueObjects;

namespace ECommerce.UnitTests.ValueObjects;

// BR-19: Para tutarı hiçbir zaman negatif olamaz (docs/domain.md).
public class MoneyTests
{
    [Fact]
    public void Constructor_NegativeAmount_Throws()
    {
        Should.Throw<ArgumentException>(() => new Money(-0.01m, "TRY"));
    }

    [Fact]
    public void Constructor_ZeroAmount_Succeeds()
    {
        var money = new Money(0m, "TRY");

        money.Amount.ShouldBe(0m);
        money.Currency.ShouldBe("TRY");
    }
}
