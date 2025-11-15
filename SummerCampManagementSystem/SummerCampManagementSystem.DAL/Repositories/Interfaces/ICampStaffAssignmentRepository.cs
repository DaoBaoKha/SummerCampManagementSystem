using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface ICampStaffAssignmentRepository : IGenericRepository<CampStaffAssignment>
    {
        Task<IEnumerable<UserAccount>> GetAvailableStaffByCampIdAsync(DateTime? start, DateTime? end);
        Task<IEnumerable<UserAccount>> GetAvailableStaffByCampForActivityAsync(int campId);
    }
}
