using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface ITransportScheduleRepository : IGenericRepository<TransportSchedule>   
    {
        IQueryable<TransportSchedule> GetSchedulesWithIncludes();
        Task<IEnumerable<TransportSchedule>> GetSchedulesByCamperIdAsync(int camperId);
        Task<IEnumerable<TransportSchedule>> GetSchedulesByCamperAndCampIdAsync(int camperId, int campId);
        
        // get schedules for staff by staffId (from TransportStaffAssignment)
        Task<IEnumerable<TransportSchedule>> GetSchedulesByStaffIdAsync(int staffId);
        
        // get schedule with staff details (includes TransportStaffAssignments)
        Task<TransportSchedule?> GetScheduleWithStaffDetailsAsync(int scheduleId);
    }
}
