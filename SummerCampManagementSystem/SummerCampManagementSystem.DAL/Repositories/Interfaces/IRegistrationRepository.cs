using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IRegistrationRepository : IGenericRepository<Registration>
    {
        Task<Registration?> GetWithDetailsForRefundAsync(int registrationId);

        Task<Registration?> GetDetailsForStatusUpdateAsync(int registrationId);

        Task<Registration?> GetWithCampersAsync(int id);

        Task<Registration?> GetForUpdateAsync(int id);

        Task<Registration?> GetFullDetailsAsync(int id);

        Task<IEnumerable<Registration>> GetAllWithDetailsAsync();

        Task<IEnumerable<Registration>> GetByStatusAsync(string status);

        Task<Registration?> GetForPaymentAsync(int id);

        Task<bool> IsCamperRegisteredAsync(int campId, int camperId);

        Task<IEnumerable<Registration>> GetHistoryByUserIdAsync(int userId);
        Task<IEnumerable<Registration>> GetByCampIdAsync(int campId);


        Task<decimal> GetTotalRevenueAsync(int campId);
        Task<int> GetPendingApprovalsCountAsync(int campId);
        Task<double> GetCancellationRateAsync(int campId);
        Task<List<(DateTime Date, int Count, decimal Revenue)>> GetRegistrationTrendAsync(int campId);
        Task<Dictionary<string, int>> GetStatusDistributionAsync(int campId);
        Task<List<(int RegistrationId, string CamperName, DateTime RegistrationDate, string Status, decimal Amount, string Avatar)>> GetRecentRegistrationsAsync(int campId, int limit);
    }
}
