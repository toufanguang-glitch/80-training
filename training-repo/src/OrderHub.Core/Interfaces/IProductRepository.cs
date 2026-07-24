using OrderHub.Core.Domain;

namespace OrderHub.Core.Interfaces;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync();
    Task<IReadOnlyList<Product>> GetActiveAsync();
    Task<IReadOnlyList<LowStockProduct>> GetLowStockAsync(int threshold, DateTime salesSince);
    Task<Product?> GetByIdAsync(int id);
    Task SaveChangesAsync();
}
