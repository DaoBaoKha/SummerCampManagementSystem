using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IUserAccountRepository : IGenericRepository<UserAccount>
    {
        // admin dashboard methods
        Task<int> GetTotalCustomersAsync();
        Task<Dictionary<string, int>> GetWorkforceDistributionAsync();
        Task<List<(DateTime Date, int Count)>> GetNewCustomerGrowthAsync(int days);
        Task<List<(int UserId, string FullName, string Email, string Role, DateTime RegisteredDate)>> GetRecentUsersAsync(int limit);
    }
}
