using ASK.Domain.Entities;
using ASK.Domain.Interfaces.Repositories;
using ASK.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASK.Infrastructure.Persistence.Repositories;

public class ProductRepository(AppDbContext context) : Repository<Product>(context), IProductRepository
{
    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == 1, cancellationToken);

    public async Task<(IReadOnlyList<Product> Items, int Total)> GetPagedAsync(
        int? categoryId, int? brandId, bool? isNew, bool? isFeatured, bool? isDealOfTheDay, bool? inStockOnly,
        string? search, int page, int limit, bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .AsNoTracking();

        if (activeOnly)
            query = query.Where(p => p.Status == 1);

        if (categoryId.HasValue)
        {
            var allCategories = await context.Categories
                .Select(c => new { c.Id, c.ParentCategoryId })
                .ToListAsync(cancellationToken);

            var categoryIds = new List<int> { categoryId.Value };
            for (int i = 0; i < categoryIds.Count; i++)
            {
                var currentId = categoryIds[i];
                var childIds = allCategories
                    .Where(c => c.ParentCategoryId == currentId)
                    .Select(c => c.Id);

                foreach (var childId in childIds)
                {
                    if (!categoryIds.Contains(childId))
                        categoryIds.Add(childId);
                }
            }

            query = query.Where(p => categoryIds.Contains(p.CategoryId));
        }

        if (brandId.HasValue)
            query = query.Where(p => p.BrandId == brandId.Value);

        if (isNew.HasValue)
            query = query.Where(p => p.IsNew == isNew.Value);

        if (isFeatured.HasValue)
            query = query.Where(p => p.IsFeatured == isFeatured.Value);

        if (isDealOfTheDay.HasValue)
            query = query.Where(p => p.IsDealOfTheDay == isDealOfTheDay.Value);

        if (inStockOnly.HasValue && inStockOnly.Value)
            query = query.Where(p => p.Stock > 0);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(q) ||
                p.Code.ToLower().Contains(q) ||
                (p.Barcode != null && p.Barcode.Contains(q)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<Product>> GetFeaturedAsync(CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.IsFeatured && p.Status == 1)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Product>> GetNewAsync(CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.IsNew && p.Status == 1)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Product>> GetLowStockAsync(int maxStock = 10, CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.Status == 1 && p.Stock > 0 && p.Stock <= maxStock)
            .OrderBy(p => p.Stock)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<bool> SlugExistsAsync(
        string slug, int? excludeId = null, CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(
            p => p.Slug == slug && (excludeId == null || p.Id != excludeId),
            cancellationToken);

    public override async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
}
