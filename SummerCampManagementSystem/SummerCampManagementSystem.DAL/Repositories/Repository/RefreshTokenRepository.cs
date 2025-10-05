using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;


namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
    {
        private readonly CampEaseDatabaseContext _context;

        public RefreshTokenRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }
        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            return await _context.RefreshTokens.Include(rt => rt.userId)
                .FirstOrDefaultAsync(rt => rt.token == token && !rt.isRevoked && rt.expiresAt > DateTime.UtcNow);
        }

        public async Task RevokeAllUserTokensAsync(int userId)
        {
            var tokens = await _context.RefreshTokens
                .Where(rt => rt.userId == userId && !rt.isRevoked)
                .ToListAsync();
            foreach (var token in tokens)
            {
                token.isRevoked = true;
            }

            _context.RefreshTokens.UpdateRange(tokens);
        }
    }
}
