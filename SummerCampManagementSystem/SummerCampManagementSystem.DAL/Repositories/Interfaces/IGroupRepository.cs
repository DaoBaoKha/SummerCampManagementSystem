using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IGroupRepository : IGenericRepository<Group>
    {
        Task<IEnumerable<Group>> GetAllCamperGroups();
        Task<Group?> GetCamperGroupById(int id);
        Task<bool> isSupervisor(int staffId, int campId);
        Task<IEnumerable<Group>> GetByCampIdAsync(int campId);
        Task<Group?> GetGroupBySupervisorIdAsync(int supervisorId, int campId);
        Task<IEnumerable<Group>> GetGroupsByActivityScheduleIdAsync(int activityScheduleId);
        Task<Group?> GetGroupByCamperAndCamp(int camperId, int campId);
        Task<List<(string Name, int Current, int Max)>> GetCapacityAlertsByCampAsync(int campId);
    }
}
