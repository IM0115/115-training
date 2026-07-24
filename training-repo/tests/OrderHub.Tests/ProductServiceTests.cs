using OrderHub.Core.Domain;

namespace OrderHub.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task GetAll_ReturnsAllProductsIncludingInactive()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001");
        TestSetup.AddProduct(db, sku: "SKU-A002", isActive: false);

        var products = await service.GetAllAsync();

        Assert.Equal(2, products.Count);
    }

    [Fact]
    public async Task GetActive_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001");
        TestSetup.AddProduct(db, sku: "SKU-A002", isActive: false);

        var products = await service.GetActiveAsync();

        Assert.All(products, p => Assert.True(p.IsActive));
        Assert.Single(products);
    }

    [Fact]
    public async Task GetLowStock_FiltersBelowThresholdAndSortsByStockAscending()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-LOW3", stock: 3);
        TestSetup.AddProduct(db, sku: "SKU-LOW1", stock: 1);
        TestSetup.AddProduct(db, sku: "SKU-EQ10", stock: 10);   // 剛好等於門檻，不算低庫存（< 而非 <=）
        TestSetup.AddProduct(db, sku: "SKU-HI20", stock: 20);

        var result = await service.GetLowStockAsync(10);

        Assert.True(result.Success);
        var items = result.Value!;
        Assert.Equal(2, items.Count);
        Assert.Equal(new[] { "SKU-LOW1", "SKU-LOW3" }, items.Select(i => i.Product.Sku)); // 庫存升冪
    }

    [Fact]
    public async Task GetLowStock_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-ACT", stock: 2, isActive: true);
        TestSetup.AddProduct(db, sku: "SKU-OFF", stock: 2, isActive: false);

        var result = await service.GetLowStockAsync(10);

        Assert.True(result.Success);
        var item = Assert.Single(result.Value!);
        Assert.Equal("SKU-ACT", item.Product.Sku);
    }

    [Fact]
    public async Task GetLowStock_SoldLast30Days_ExcludesCancelledAndOrdersOlderThan30Days()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, sku: "SKU-SOLD", stock: 2);
        var now = DateTime.UtcNow;

        TestSetup.AddOrder(db, customer.Id, now.AddDays(-1), OrderStatus.Confirmed, (product.Id, 4));   // 計入
        TestSetup.AddOrder(db, customer.Id, now.AddDays(-2), OrderStatus.Shipped, (product.Id, 3));     // 計入
        TestSetup.AddOrder(db, customer.Id, now.AddDays(-3), OrderStatus.Cancelled, (product.Id, 100)); // 排除：已取消
        TestSetup.AddOrder(db, customer.Id, now.AddDays(-40), OrderStatus.Confirmed, (product.Id, 50)); // 排除：超過 30 天

        var result = await service.GetLowStockAsync(10);

        Assert.True(result.Success);
        var item = Assert.Single(result.Value!);
        Assert.Equal(7, item.SoldLast30Days); // 只算近 30 天且非取消：4 + 3
    }

    [Fact]
    public async Task GetLowStock_ThresholdNotPositive_Fails()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);

        var zero = await service.GetLowStockAsync(0);
        var negative = await service.GetLowStockAsync(-1);

        Assert.False(zero.Success);
        Assert.False(negative.Success);
    }
}
