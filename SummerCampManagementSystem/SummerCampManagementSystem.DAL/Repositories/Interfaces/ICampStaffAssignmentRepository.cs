using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface ICampStaffAssignmentRepository : IGenericRepository<CampStaffAssignment>
    {
        Task<IEnumerable<UserAccount>> GetAvailableStaffManagerByCampIdAsync(DateTime? start, DateTime? end);
        Task<IEnumerable<UserAccount>> GetAvailableStaffByCampId(int campId);
        Task<bool> IsStaffBusyInAnyCampAsync(int staffId, DateOnly date);
        Task<IEnumerable<int>> GetStaffIdsByCampIdAsync(int campId);
        Task<IEnumerable<int>> GetBusyStaffIdsInOtherActiveCampAsync(DateTime checkDate, int currentCampId);
        Task<bool> IsStaffBusyInOtherActiveCampAsync(int staffId, DateTime checkDate, int currentCampId);
    }
}
