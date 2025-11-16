using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface ICampStaffAssignmentRepository : IGenericRepository<CampStaffAssignment>
    {
        Task<IEnumerable<UserAccount>> GetAvailableStaffManagerByCampIdAsync(DateTime? start, DateTime? end);
        Task<IEnumerable<UserAccount>> GetAvailableStaffByCampId(int campId);

    }
}
