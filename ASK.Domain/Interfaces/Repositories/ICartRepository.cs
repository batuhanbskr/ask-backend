using ASK.Domain.Entities;

namespace ASK.Domain.Interfaces.Repositories;

public interface ICartRepository : IRepository<Cart>
{
    Task<Cart?> GetByUserIdWithItemsAsync(int userId, CancellationToken cancellationToken = default);
}
