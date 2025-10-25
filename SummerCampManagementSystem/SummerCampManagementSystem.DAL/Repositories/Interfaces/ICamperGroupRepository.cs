using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface ICamperGroupRepository : IGenericRepository<CamperGroup>
    {
        Task<bool> isSupervisor(int staffId);
        Task<IEnumerable<CamperGroup>> GetByCampIdAsync(int campId);
    }
}
