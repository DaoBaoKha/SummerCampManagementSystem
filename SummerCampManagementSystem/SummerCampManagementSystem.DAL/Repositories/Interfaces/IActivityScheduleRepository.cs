using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IActivityScheduleRepository : IGenericRepository<ActivitySchedule>
    {
        Task<bool> IsTimeOverlapAsync(int? campId, DateTime start, DateTime end);
        Task<bool> ExistsInSameTimeAndLocationAsync(int locationId, DateTime start, DateTime end);
        Task<bool> IsStaffBusyAsync(int staffId, DateTime start, DateTime end);
        Task<ActivitySchedule?> GetByIdWithActivityAsync(int id);
        Task<IEnumerable<ActivitySchedule>> GetByCampAndStaffAsync(int campId, int staffId);
        Task<IEnumerable<ActivitySchedule>> GetAllWithActivityAndAttendanceAsync(int campId, int camperId);
        Task<IEnumerable<ActivitySchedule>> GetOptionalScheduleByCampIdAsync(int campId);
        Task<IEnumerable<ActivitySchedule>> GetCoreScheduleByCampIdAsync(int campId);
    }
}
