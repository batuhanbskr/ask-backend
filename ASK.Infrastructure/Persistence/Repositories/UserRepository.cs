using ASK.Domain.Entities;
using ASK.Domain.Interfaces.Repositories;
using ASK.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASK.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext context) : Repository<User>(context), IUserRepository
{
    public override async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await DbSet.Include(u => u.CategoryDiscounts).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await DbSet.Include(u => u.SalesRepresentative).Include(u => u.CategoryDiscounts).FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(u => u.Email == email, cancellationToken);

    public async Task<User?> GetWithSalesRepresentativeByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await DbSet.Include(u => u.SalesRepresentative).Include(u => u.CategoryDiscounts).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
}

public class BrandRepository(AppDbContext context) : Repository<Brand>(context), IBrandRepository
{
}

public class ContactMessageRepository(AppDbContext context) : Repository<ContactMessage>(context), IContactMessageRepository
{
}
