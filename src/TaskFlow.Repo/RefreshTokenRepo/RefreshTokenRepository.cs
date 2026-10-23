using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskFlow.Entities.Authentication;
using TaskFlow.Repo.Data;

namespace TaskFlow.Repo.RefreshTokenRepo
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public RefreshTokenRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task AddAsync(RefreshToken refreshToken , CancellationToken cancellationToken = default)
        {
            await _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
           
        }
        public  Task<RefreshToken?> FindByTokenHashAsync (string tokenHash,CancellationToken cancellationToken = default)
        {
            return  _dbContext.RefreshTokens.Include(token=>token.User).SingleOrDefaultAsync(token=>token.TokenHash == tokenHash, cancellationToken);
        }

        public Task<List<RefreshToken>> GetActiveByFamilyIdAsync(Guid familyId , CancellationToken cancellationToken = default)
        {
            return _dbContext.RefreshTokens
                .Where(token => token.FamilyId == familyId && token.RevokedAtUtc == null && token.ExpiresAtUtc > DateTime.UtcNow)
                .ToListAsync(cancellationToken);
        }

        public Task<List<RefreshToken>> GetActiveByUserIdAsync(string userId , CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            return _dbContext.RefreshTokens.Where(token=>token.UserId == userId && token.RevokedAtUtc == null && token.ExpiresAtUtc > now)
                .ToListAsync(cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

