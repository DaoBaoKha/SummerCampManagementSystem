using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IUserRepository : IGenericRepository<UserAccount>
    {
        Task<UserAccount?> GetUserByEmail(string email);
        Task<UserAccount?> GetByGoogleIdAsync(string googleId);
    }
}
