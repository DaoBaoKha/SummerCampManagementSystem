using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface ICamperGroupRepository : IGenericRepository<CamperGroup>
    {
        Task<bool> isSupervisor(int staffId);
        Task<IEnumerable<CamperGroup>> GetByCampIdAsync(int campId);
        Task<CamperGroup?> GetGroupBySupervisorIdAsync(int supervisorId, int campId);
        Task<IEnumerable<CamperGroup>> GetGroupsByActivityScheduleIdAsync(int activityScheduleId);
    }
}
