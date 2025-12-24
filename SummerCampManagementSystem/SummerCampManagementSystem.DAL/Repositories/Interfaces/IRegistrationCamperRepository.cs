using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IRegistrationCamperRepository : IGenericRepository<RegistrationCamper>
    {
        Task<RegistrationCamper?> GetByCamperId(int camperId);
        Task<RegistrationCamper?> GetByCamperIdAsync(int camperId);
        Task<RegistrationCamper?> GetByCamperIdAndCampIdAsync(int camperId, int campId);
        Task<IEnumerable<RegistrationCamper>> GetByCampIdAsync(int campId);
        Task<RegistrationCamper?> GetByCompositeKeyWithIncludesAsync(int registrationId, int camperId);
    }
}
