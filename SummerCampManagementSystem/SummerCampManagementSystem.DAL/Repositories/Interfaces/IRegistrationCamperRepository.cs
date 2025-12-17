using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IRegistrationCamperRepository : IGenericRepository<RegistrationCamper>
    {
        Task<RegistrationCamper?> GetByCamperId(int camperId);
        Task<RegistrationCamper?> GetByCamperIdAsync(int camperId);
        Task<IEnumerable<RegistrationCamper>> GetByCampIdAsync(int campId);
    }
}
