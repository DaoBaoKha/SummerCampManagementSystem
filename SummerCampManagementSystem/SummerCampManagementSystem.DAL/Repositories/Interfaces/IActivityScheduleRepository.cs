using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IActivityScheduleRepository : IGenericRepository<ActivitySchedule>
    {
        Task<IEnumerable<ActivitySchedule>> GetAllSchedule();
        Task<ActivitySchedule?> GetScheduleById(int id);
        Task<bool> IsTimeOverlapAsync(int? campId, DateTime start, DateTime end, int? excludeScheduleId = null);
        Task<bool> ExistsInSameTimeAndLocationAsync(int locationId, DateTime start, DateTime end, int? excludeScheduleId = null);
        Task<bool> IsStaffBusyAsync(int staffId, DateTime start, DateTime end, int? excludeScheduleId = null);
        Task<ActivitySchedule?> GetByIdWithActivityAsync(int id);
        Task<IEnumerable<ActivitySchedule>> GetAllWithActivityAndAttendanceAsync(int campId, int camperId);
        Task<IEnumerable<ActivitySchedule>> GetOptionalScheduleByCampIdAsync(int campId);
        Task<IEnumerable<ActivitySchedule>> GetCoreScheduleByCampIdAsync(int campId);
        Task<IEnumerable<ActivitySchedule>> GetScheduleByCampIdAsync(int campId);
        Task<IEnumerable<ActivitySchedule>> GetActivitySchedulesByDateAsync(DateTime fromDate, DateTime toDate);
        Task<bool> IsStaffOfActivitySchedule(int staffId, int activityScheduleId);
        Task<IEnumerable<ActivitySchedule>> GetByCampAndStaffAsync(int campId, int staffId);      
        Task<IEnumerable<ActivitySchedule>> GetAllSchedulesByStaffIdAsync(int staffId, int campId);
        Task<ActivitySchedule?> GetOptionalByCoreAsync(int coreActivityId);
        Task<bool> IsCamperofCamp(int campId, int camperId);
        Task<IEnumerable<ActivitySchedule>> GetSchedulesByGroupStaffAsync(int campId, int staffId);
        Task<IEnumerable<ActivitySchedule>> GetOptionalSchedulesByCamperAsync(int camperId);
        IQueryable<ActivitySchedule> GetQueryableWithBaseIncludes();
        Task<IEnumerable<ActivitySchedule>> GetCheckInCheckoutByCampAndStaffAsync(int campId, int staffId);
        Task<IEnumerable<int>> GetBusyStaffIdsInActivityAsync(DateTime startUtc, DateTime endUtc);
        Task<IEnumerable<ActivitySchedule>> GetAllTypeSchedulesByStaffAsync(int campId, int staffId);
    }
}
