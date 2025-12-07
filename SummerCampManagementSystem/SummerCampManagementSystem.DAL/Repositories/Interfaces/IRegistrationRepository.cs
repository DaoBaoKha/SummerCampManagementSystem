using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IRegistrationRepository : IGenericRepository<Registration>
    {
        Task<Registration?> GetWithDetailsForRefundAsync(int registrationId);

    }
}
