using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IBankUserRepository : IGenericRepository<BankUser>
    {
        Task<IEnumerable<BankUser>> GetByUserIdAsync(int userId);
        Task<BankUser?> GetPrimaryByUserIdAsync(int userId);
    }
}
