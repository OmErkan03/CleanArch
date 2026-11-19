using ECommerce.Application.Services.Repositories;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories
{
    // IOperationClaimRepository'nin EF Core ile yazılmış implementasyonu.
    public class OperationClaimRepository : IOperationClaimRepository
    {
        private readonly UludagDbContext _context;

        public OperationClaimRepository(UludagDbContext context)
        {
            _context = context;
        }

        public async Task<OperationClaim?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.OperationClaims.FirstOrDefaultAsync(oc => oc.Id == id, cancellationToken);
        }
    }
}
