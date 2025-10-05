using SummerCampManagementSystem.DAL.Models;


namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IRefreshTokenRepository : IGenericRepository<RefreshToken>
    {
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task RevokeAllUserTokensAsync(int userId);

    }
}
