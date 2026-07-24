using Microsoft.EntityFrameworkCore;
using OrderHub.Core.Common;
using OrderHub.Core.Domain;
using OrderHub.Core.Interfaces;
using OrderHub.Infrastructure.Data;

namespace OrderHub.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderHubDbContext _db;

    public OrderRepository(OrderHubDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<Order>> GetPagedAsync(
        int page,
        int pageSize,
        OrderStatus? status,
        OrderSortField sort,
        SortDirection direction)
    {
        var query = _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        var totalCount = await query.CountAsync();

        var orderedQuery = (sort, direction) switch
        {
            (OrderSortField.Id, SortDirection.Ascending) => query.OrderBy(o => o.Id),
            (OrderSortField.Id, SortDirection.Descending) => query.OrderByDescending(o => o.Id),
            (OrderSortField.Customer, SortDirection.Ascending) => query.OrderBy(o => o.Customer!.Name),
            (OrderSortField.Customer, SortDirection.Descending) => query.OrderByDescending(o => o.Customer!.Name),
            (OrderSortField.Status, SortDirection.Ascending) => query.OrderBy(o => o.Status),
            (OrderSortField.Status, SortDirection.Descending) => query.OrderByDescending(o => o.Status),
            (OrderSortField.Total, SortDirection.Ascending) => query.OrderBy(o => o.Items.Sum(i => i.UnitPriceSnapshot * i.Quantity)),
            (OrderSortField.Total, SortDirection.Descending) => query.OrderByDescending(o => o.Items.Sum(i => i.UnitPriceSnapshot * i.Quantity)),
            (OrderSortField.ItemCount, SortDirection.Ascending) => query.OrderBy(o => o.Items.Count),
            (OrderSortField.ItemCount, SortDirection.Descending) => query.OrderByDescending(o => o.Items.Count),
            (OrderSortField.CreatedAt, SortDirection.Ascending) => query.OrderBy(o => o.CreatedAt),
            _ => query.OrderByDescending(o => o.CreatedAt)
        };

        var items = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Order>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public Task<Order?> GetWithDetailsAsync(int id) =>
        _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<IReadOnlyList<Order>> GetByCustomerAsync(int customerId) =>
        await _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(Order order) => await _db.Orders.AddAsync(order);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
