using OrderHub.Core.Common;
using OrderHub.Core.Domain;

namespace OrderHub.Core.Interfaces;

public interface IOrderRepository
{
    Task<PagedResult<Order>> GetPagedAsync(
        int page,
        int pageSize,
        OrderStatus? status,
        OrderSortField sort,
        SortDirection direction);
    Task<Order?> GetWithDetailsAsync(int id);
    Task<IReadOnlyList<Order>> GetByCustomerAsync(int customerId);
    Task AddAsync(Order order);
    Task SaveChangesAsync();
}
