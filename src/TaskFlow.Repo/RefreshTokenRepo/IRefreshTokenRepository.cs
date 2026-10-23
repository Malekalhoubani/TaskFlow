using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TaskFlow.Entities.Authentication;

namespace TaskFlow.Repo.RefreshTokenRepo
{
    public interface IRefreshTokenRepository
    {
        Task AddAsync(RefreshToken refreshToken , CancellationToken cancellationToken = default);
        Task <RefreshToken?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
        Task<List<RefreshToken>> GetActiveByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default);
        Task<List<RefreshToken>> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
