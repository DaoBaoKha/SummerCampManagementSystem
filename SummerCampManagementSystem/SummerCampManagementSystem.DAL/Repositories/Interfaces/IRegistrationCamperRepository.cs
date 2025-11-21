using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IRegistrationCamperRepository : IGenericRepository<RegistrationCamper>
    {
        Task<RegistrationCamper?> GetByCamperId(int camperId);
    }
}
