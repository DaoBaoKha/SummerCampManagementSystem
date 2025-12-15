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

        // Lấy danh sách đăng ký theo CampId
        Task<IEnumerable<Registration>> GetByCampIdAsync(int campId);
    }
}
