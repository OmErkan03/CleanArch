using ECommerce.Application.Services.Repositories;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories
{
    // IUserRepository'nin EF Core ile yazılmış implementasyonu.
    public class UserRepository : IUserRepository
    {
        private readonly UludagDbContext _context;

        public UserRepository(UludagDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }

        public async Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }

        public async Task<User?> GetByIdWithOperationClaimsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.OperationClaims)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }

        public async Task<List<string>> GetOperationClaimNamesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            // Ara tablo (UserOperationClaims) üzerinden JOIN; yalnızca claim adları çekilir.
            return await _context.Users
                .Where(u => u.Id == userId)
                .SelectMany(u => u.OperationClaims)
                .Select(oc => oc.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            // AddAsync sırasında EF, Guid tipindeki Id'yi otomatik üretir.
            await _context.Users.AddAsync(user, cancellationToken);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => _context.SaveChangesAsync(cancellationToken);
    }
}
