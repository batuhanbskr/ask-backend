using ASK.Domain.Entities;
using ASK.Domain.Interfaces.Repositories;
using ASK.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASK.Infrastructure.Persistence.Repositories;

public class OrderRepository(AppDbContext context) : Repository<Order>(context), IOrderRepository
{
    public async Task<Order?> GetByIdWithItemsAsync(int id, CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(o => o.OrderItems)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await DbSet.CountAsync(
            o => o.CreatedAt.Date == DateTime.UtcNow.Date, cancellationToken);
        return $"ASK-{today}-{(count + 1):D5}";
    }
}

public class CartRepository(AppDbContext context) : Repository<Cart>(context), ICartRepository
{
    public async Task<Cart?> GetByUserIdWithItemsAsync(int userId, CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
}

public class RefreshTokenRepository(AppDbContext context) : Repository<RefreshToken>(context), IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetActiveTokenAsync(string token, CancellationToken cancellationToken = default) =>
        await DbSet.FirstOrDefaultAsync(
            rt => rt.Token == token && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow,
            cancellationToken);

    public async Task RevokeAllUserTokensAsync(int userId, CancellationToken cancellationToken = default)
    {
        var tokens = await DbSet
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }
    }
}
