using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface ICampRepository : IGenericRepository<Camp>
    {
        Task<IEnumerable<Camp>> GetCampsByTypeAsync(int campTypeId);
        Task<IEnumerable<Camp>> GetCampsByStaffIdAsync(int staffId);

        // admin dashboard methods
        Task<int> GetActiveCampsCountAsync();
        Task<Dictionary<string, int>> GetCampStatusDistributionAsync();
        Task<List<(string Month, decimal Revenue)>> GetMonthlyRevenueAsync(int months);
        Task<List<(int CampId, string Name, string ManagerName, DateTime SubmittedDate, string Status)>> GetPendingCampsAsync();
    }
}
