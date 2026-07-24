using OrderHub.Core.Domain;

namespace OrderHub.Tests;

public class ProductServiceLowStockTests
{
    [Fact]
    public async Task GetLowStock_WithThreshold_ReturnsMatchingProductsByStockAscending()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, stock: 8, sku: "SKU-8");
        TestSetup.AddProduct(db, stock: 2, sku: "SKU-2");
        TestSetup.AddProduct(db, stock: 5, sku: "SKU-5");
        TestSetup.AddProduct(db, stock: 10, sku: "SKU-10");

        var products = await service.GetLowStockAsync(10);

        Assert.Equal(new[] { "SKU-2", "SKU-5", "SKU-8" }, products.Select(p => p.Sku));
    }

    [Fact]
    public async Task GetLowStock_InactiveProduct_ExcludesProduct()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, stock: 2, sku: "ACTIVE");
        TestSetup.AddProduct(db, stock: 1, isActive: false, sku: "INACTIVE");

        var products = await service.GetLowStockAsync(10);

        var product = Assert.Single(products);
        Assert.Equal("ACTIVE", product.Sku);
    }

    [Fact]
    public async Task GetLowStock_RecentSales_ExcludesCancelledAndOlderOrders()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, stock: 2, sku: "LOW-STOCK");
        var now = DateTime.UtcNow;

        var recentOrder = new Order { CustomerId = customer.Id, Status = OrderStatus.Confirmed, CreatedAt = now.AddDays(-5) };
        var cancelledOrder = new Order { CustomerId = customer.Id, Status = OrderStatus.Cancelled, CreatedAt = now.AddDays(-5) };
        var olderOrder = new Order { CustomerId = customer.Id, Status = OrderStatus.Shipped, CreatedAt = now.AddDays(-31) };
        db.Orders.AddRange(recentOrder, cancelledOrder, olderOrder);
        await db.SaveChangesAsync();

        db.OrderItems.AddRange(
            new OrderItem { OrderId = recentOrder.Id, ProductId = product.Id, Quantity = 3, UnitPriceSnapshot = 100m },
            new OrderItem { OrderId = cancelledOrder.Id, ProductId = product.Id, Quantity = 4, UnitPriceSnapshot = 100m },
            new OrderItem { OrderId = olderOrder.Id, ProductId = product.Id, Quantity = 5, UnitPriceSnapshot = 100m });
        await db.SaveChangesAsync();

        var lowStockProduct = Assert.Single(await service.GetLowStockAsync(10));

        Assert.Equal(3, lowStockProduct.RecentSalesQuantity);
    }
}
