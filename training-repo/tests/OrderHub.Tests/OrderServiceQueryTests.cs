using OrderHub.Core.Domain;

namespace OrderHub.Tests;

public class OrderServiceQueryTests
{
    public static IEnumerable<object[]> OrderSortCases()
    {
        yield return new object[] { OrderSortField.Id, SortDirection.Ascending, 1 };
        yield return new object[] { OrderSortField.Id, SortDirection.Descending, 2 };
        yield return new object[] { OrderSortField.Customer, SortDirection.Ascending, 2 };
        yield return new object[] { OrderSortField.Customer, SortDirection.Descending, 1 };
        yield return new object[] { OrderSortField.Status, SortDirection.Ascending, 2 };
        yield return new object[] { OrderSortField.Status, SortDirection.Descending, 1 };
        yield return new object[] { OrderSortField.Total, SortDirection.Ascending, 1 };
        yield return new object[] { OrderSortField.Total, SortDirection.Descending, 2 };
        yield return new object[] { OrderSortField.ItemCount, SortDirection.Ascending, 1 };
        yield return new object[] { OrderSortField.ItemCount, SortDirection.Descending, 2 };
        yield return new object[] { OrderSortField.CreatedAt, SortDirection.Ascending, 1 };
        yield return new object[] { OrderSortField.CreatedAt, SortDirection.Descending, 2 };
    }

    [Theory]
    [MemberData(nameof(OrderSortCases))]
    public async Task GetOrders_WithSortField_ReturnsOrdersInRequestedDirection(
        OrderSortField sort,
        SortDirection direction,
        int expectedFirstOrderId)
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customerZ = TestSetup.AddCustomer(db, name: "Zeta");
        var customerA = TestSetup.AddCustomer(db, name: "Alpha");
        var createdAt = DateTime.UtcNow;

        db.Orders.AddRange(
            new Order
            {
                Id = 1,
                CustomerId = customerZ.Id,
                Status = OrderStatus.Shipped,
                CreatedAt = createdAt.AddMinutes(-1),
                Items = { new OrderItem { ProductId = 1, Quantity = 1, UnitPriceSnapshot = 20m } }
            },
            new Order
            {
                Id = 2,
                CustomerId = customerA.Id,
                Status = OrderStatus.Pending,
                CreatedAt = createdAt,
                Items =
                {
                    new OrderItem { ProductId = 1, Quantity = 1, UnitPriceSnapshot = 30m },
                    new OrderItem { ProductId = 2, Quantity = 1, UnitPriceSnapshot = 30m }
                }
            });
        db.SaveChanges();

        var result = await service.GetOrdersAsync(1, 20, null, sort, direction);

        Assert.Equal(expectedFirstOrderId, result.Items.First().Id);
    }

    [Fact]
    public async Task GetOrders_DefaultSort_ReturnsNewestOrdersOnFirstPage()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customer = TestSetup.AddCustomer(db);
        var createdAt = DateTime.UtcNow;

        db.Orders.AddRange(
            new Order { CustomerId = customer.Id, Status = OrderStatus.Pending, CreatedAt = createdAt.AddMinutes(-1) },
            new Order { CustomerId = customer.Id, Status = OrderStatus.Pending, CreatedAt = createdAt });
        db.SaveChanges();

        var result = await service.GetOrdersAsync(1, 20, null);

        Assert.Equal(2, result.Items.Count);
        Assert.True(result.Items[0].CreatedAt > result.Items[1].CreatedAt);
    }

    [Fact]
    public async Task GetOrders_WithStatusFilter_ReturnsOnlyMatchingStatus()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customer = TestSetup.AddCustomer(db);

        db.Orders.AddRange(
            new Order { CustomerId = customer.Id, Status = OrderStatus.Pending, CreatedAt = DateTime.UtcNow },
            new Order { CustomerId = customer.Id, Status = OrderStatus.Shipped, CreatedAt = DateTime.UtcNow },
            new Order { CustomerId = customer.Id, Status = OrderStatus.Shipped, CreatedAt = DateTime.UtcNow });
        db.SaveChanges();

        var result = await service.GetOrdersAsync(1, 20, OrderStatus.Shipped);

        Assert.All(result.Items, o => Assert.Equal(OrderStatus.Shipped, o.Status));
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetOrders_ReportsTotalCountAndTotalPages()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customer = TestSetup.AddCustomer(db);

        for (var i = 0; i < 45; i++)
            db.Orders.Add(new Order { CustomerId = customer.Id, Status = OrderStatus.Confirmed, CreatedAt = DateTime.UtcNow.AddMinutes(-i) });
        db.SaveChanges();

        var result = await service.GetOrdersAsync(1, 20, null);

        Assert.Equal(45, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task GetCustomerOrders_ReturnsOnlyThatCustomersOrders()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateOrderService(db);
        var customerA = TestSetup.AddCustomer(db, name: "客戶A");
        var customerB = TestSetup.AddCustomer(db, name: "客戶B");

        db.Orders.AddRange(
            new Order { CustomerId = customerA.Id, Status = OrderStatus.Pending, CreatedAt = DateTime.UtcNow },
            new Order { CustomerId = customerB.Id, Status = OrderStatus.Pending, CreatedAt = DateTime.UtcNow },
            new Order { CustomerId = customerA.Id, Status = OrderStatus.Shipped, CreatedAt = DateTime.UtcNow });
        db.SaveChanges();

        var orders = await service.GetCustomerOrdersAsync(customerA.Id);

        Assert.Equal(2, orders.Count);
        Assert.All(orders, o => Assert.Equal(customerA.Id, o.CustomerId));
    }
}
