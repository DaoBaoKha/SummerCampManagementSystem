using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.GenericRepository;

namespace SummerCampManagementSystem.DAL.Repositories.UserRepository
{
    public interface IUserRepository : IGenericRepository<UserAccount>
    {
        Task<UserAccount?> GetUserAccount(string email, string password);
    }
}
