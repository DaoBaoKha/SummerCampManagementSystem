using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly CampEaseDatabaseContext _context;
        public IUserRepository Users { get; }
        public IRefreshTokenRepository RefreshTokens { get; }

        public UnitOfWork(CampEaseDatabaseContext context, IUserRepository userRepository, IRefreshTokenRepository refreshTokenRepository)
        {
            _context = context;
            Users = userRepository;
            RefreshTokens = refreshTokenRepository;
        }

        public async Task<int> CommitAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
