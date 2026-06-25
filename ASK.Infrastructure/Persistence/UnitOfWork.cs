using ASK.Domain.Interfaces;
using ASK.Domain.Interfaces.Repositories;
using ASK.Infrastructure.Persistence;
using ASK.Infrastructure.Persistence.Repositories;

namespace ASK.Infrastructure.Persistence;

/// <summary>
/// Unit of Work implementasyonu.
/// Tüm repository'leri tek bir DbContext transaction'ında yönetir.
/// </summary>
public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    private IUserRepository? _users;
    private IProductRepository? _products;
    private ICategoryRepository? _categories;
    private IBrandRepository? _brands;
    private IOrderRepository? _orders;
    private ICartRepository? _carts;
    private IContactMessageRepository? _contactMessages;
    private IRefreshTokenRepository? _refreshTokens;

    // Lazy initialization – yalnızca kullanıldığında oluşturulur
    public IUserRepository Users => _users ??= new UserRepository(context);
    public IProductRepository Products => _products ??= new ProductRepository(context);
    public ICategoryRepository Categories => _categories ??= new CategoryRepository(context);
    public IBrandRepository Brands => _brands ??= new BrandRepository(context);
    public IOrderRepository Orders => _orders ??= new OrderRepository(context);
    public ICartRepository Carts => _carts ??= new CartRepository(context);
    public IContactMessageRepository ContactMessages => _contactMessages ??= new ContactMessageRepository(context);
    public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new RefreshTokenRepository(context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await context.SaveChangesAsync(cancellationToken);

    public void Dispose() => context.Dispose();
}
