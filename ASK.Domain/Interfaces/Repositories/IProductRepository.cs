using ASK.Domain.Entities;

namespace ASK.Domain.Interfaces.Repositories;

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Product> Items, int Total)> GetPagedAsync(
        int? categoryId, int? brandId, bool? isNew, bool? isFeatured, bool? isDealOfTheDay, bool? inStockOnly,
        string? search, int page, int limit, bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetFeaturedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetNewAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetLowStockAsync(int maxStock = 10, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, int? excludeId = null, CancellationToken cancellationToken = default);
}
