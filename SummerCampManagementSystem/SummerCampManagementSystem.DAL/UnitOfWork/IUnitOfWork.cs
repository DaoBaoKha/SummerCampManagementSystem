using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
       IUserRepository Users { get; }
        IRefreshTokenRepository RefreshTokens { get; }

        Task<int> CommitAsync();
    }
}
