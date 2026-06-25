using ASK.Domain.Entities;
using ASK.Domain.Interfaces.Repositories;
using ASK.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASK.Infrastructure.Persistence.Repositories;

public class CategoryRepository(AppDbContext context) : Repository<Category>(context), ICategoryRepository
{
    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive, cancellationToken);

    public async Task<bool> SlugExistsAsync(
        string slug, int? excludeId = null, CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(
            c => c.Slug == slug && (excludeId == null || c.Id != excludeId),
            cancellationToken);

    public async Task<IReadOnlyList<Category>> GetAllWithProductCountAsync(CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(c => c.Products)
            .Where(c => c.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
}
