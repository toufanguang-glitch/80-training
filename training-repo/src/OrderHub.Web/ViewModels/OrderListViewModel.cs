using OrderHub.Core.Domain;

namespace OrderHub.Web.ViewModels;

public class OrderListViewModel
{
    public IReadOnlyList<OrderRowViewModel> Orders { get; set; } = Array.Empty<OrderRowViewModel>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public OrderStatus? Status { get; set; }
    public OrderSortField Sort { get; set; } = OrderSortField.CreatedAt;
    public SortDirection Direction { get; set; } = SortDirection.Descending;
}
