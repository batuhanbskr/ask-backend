using ASK.Domain.Interfaces.Repositories;

namespace ASK.Domain.Interfaces;

/// <summary>
/// Unit of Work pattern arayüzü.
/// Tüm repository'leri tek bir transaction kapsamında yönetir.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IProductRepository Products { get; }
    ICategoryRepository Categories { get; }
    IBrandRepository Brands { get; }
    IOrderRepository Orders { get; }
    ICartRepository Carts { get; }
    IContactMessageRepository ContactMessages { get; }
    IRefreshTokenRepository RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
