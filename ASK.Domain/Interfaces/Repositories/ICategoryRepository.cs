using ASK.Domain.Entities;

namespace ASK.Domain.Interfaces.Repositories;

public interface ICategoryRepository : IRepository<Category>
{
    Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetAllWithProductCountAsync(CancellationToken cancellationToken = default);
}
