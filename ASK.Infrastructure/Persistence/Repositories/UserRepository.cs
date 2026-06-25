using ASK.Domain.Entities;
using ASK.Domain.Interfaces.Repositories;
using ASK.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASK.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext context) : Repository<User>(context), IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await DbSet.Include(u => u.SalesRepresentative).FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(u => u.Email == email, cancellationToken);

    public async Task<User?> GetWithSalesRepresentativeByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await DbSet.Include(u => u.SalesRepresentative).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
}

public class BrandRepository(AppDbContext context) : Repository<Brand>(context), IBrandRepository
{
}

public class ContactMessageRepository(AppDbContext context) : Repository<ContactMessage>(context), IContactMessageRepository
{
}
