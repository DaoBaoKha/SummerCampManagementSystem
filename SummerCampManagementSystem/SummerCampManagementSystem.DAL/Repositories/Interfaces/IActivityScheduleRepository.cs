using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IActivityScheduleRepository : IGenericRepository<ActivitySchedule>
    {
        Task<bool> IsTimeOverlapAsync(int? campId, DateTime start, DateTime end, int? excludeScheduleId = null);
        Task<bool> ExistsInSameTimeAndLocationAsync(int locationId, DateTime start, DateTime end, int? excludeScheduleId = null);
        Task<bool> IsStaffBusyAsync(int staffId, DateTime start, DateTime end, int? excludeScheduleId = null);
        Task<ActivitySchedule?> GetByIdWithActivityAsync(int id);
        Task<IEnumerable<ActivitySchedule>> GetByCampAndStaffAsync(int campId, int staffId);
        Task<IEnumerable<ActivitySchedule>> GetAllWithActivityAndAttendanceAsync(int campId, int camperId);
        Task<IEnumerable<ActivitySchedule>> GetOptionalScheduleByCampIdAsync(int campId);
        Task<IEnumerable<ActivitySchedule>> GetCoreScheduleByCampIdAsync(int campId);
        Task<IEnumerable<ActivitySchedule>> GetScheduleByCampIdAsync(int campId);
        Task<IEnumerable<ActivitySchedule>> GetActivitySchedulesByDateAsync(DateTime fromDate, DateTime toDate);
        Task<bool> IsStaffOfActivitySchedule(int staffId, int activityScheduleId);
    }
}
