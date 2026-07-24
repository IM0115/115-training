using OrderHub.Core.Domain;
using OrderHub.Core.Services;

namespace OrderHub.Tests;

public class OrderServicePricingTests
{
    [Theory]
    [InlineData(CustomerTier.Standard, 0)]
    [InlineData(CustomerTier.Silver, 0.05)]
    [InlineData(CustomerTier.Gold, 0.10)]
    public void GetDiscountRate_ReturnsExpectedRate(CustomerTier tier, decimal expected)
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);

        Assert.Equal(expected, service.GetDiscountRate(tier));
    }

    [Fact]
    public void CalculateSubtotal_SumsQuantityTimesSnapshotPrice()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);

        var order = new Order
        {
            Items =
            {
                new OrderItem { Quantity = 2, UnitPriceSnapshot = 150m },
                new OrderItem { Quantity = 3, UnitPriceSnapshot = 40m }
            }
        };

        Assert.Equal(420m, service.CalculateSubtotal(order));
    }

    [Theory]
    [InlineData(CustomerTier.Standard, 1000, 1000)]
    [InlineData(CustomerTier.Silver, 1000, 950)]
    [InlineData(CustomerTier.Gold, 1000, 900)]
    public void CalculateTotal_AppliesTierDiscountOnSubtotal(CustomerTier tier, decimal unitPrice, decimal expectedTotal)
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);

        var order = new Order
        {
            Customer = new Customer { Tier = tier },
            Items = { new OrderItem { Quantity = 1, UnitPriceSnapshot = unitPrice } }
        };

        Assert.Equal(expectedTotal, service.CalculateTotal(order));
    }

    [Fact]
    public async Task CreateOrder_GoldCustomer_SnapshotKeepsRawPrice_AndTotalDiscountedOnce()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customer = TestSetup.AddCustomer(db, CustomerTier.Gold);
        var product = TestSetup.AddProduct(db, unitPrice: 1000m, stock: 10);

        var result = await service.CreateOrderAsync(customer.Id, new[] { new NewOrderLine(product.Id, 1) });
        Assert.True(result.Success);

        // 快照必須存原價，不可在建單時先折一次（bug 時會是 900）
        Assert.Equal(1000m, result.Value!.Items.Single().UnitPriceSnapshot);

        // 重新載入（含 Customer）後總額只折一次：1000 × 0.9 = 900（bug 時重複折扣會變成 810）
        var reloaded = await service.GetOrderAsync(result.Value.Id);
        Assert.Equal(900m, service.CalculateTotal(reloaded!));
    }

    [Fact]
    public void CalculateTotal_WithoutCustomer_UsesStandardRate()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);

        var order = new Order
        {
            Items = { new OrderItem { Quantity = 2, UnitPriceSnapshot = 250m } }
        };

        Assert.Equal(500m, service.CalculateTotal(order));
    }
}
