namespace OrderHub.Core.Domain;

public class LowStockProduct
{
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int StockQuantity { get; init; }
    public int RecentSalesQuantity { get; init; }
}
