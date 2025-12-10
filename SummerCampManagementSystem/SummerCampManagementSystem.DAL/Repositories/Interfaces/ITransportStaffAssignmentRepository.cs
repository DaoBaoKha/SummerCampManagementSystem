using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface ITransportStaffAssignmentRepository : IGenericRepository<TransportStaffAssignment>
    {
        Task<bool> ExistsAsync(int scheduleId, int staffId);

        Task<IEnumerable<TransportSchedule>> GetSchedulesByStaffIdAsync(int staffId);

        Task<IEnumerable<int>> GetBusyStaffIdsInOtherTransportAsync(DateOnly date, TimeOnly start, TimeOnly end);
    }
}
