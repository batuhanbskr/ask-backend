using ASK.Domain.Entities;

namespace ASK.Domain.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetWithSalesRepresentativeByIdAsync(int id, CancellationToken cancellationToken = default);
}
