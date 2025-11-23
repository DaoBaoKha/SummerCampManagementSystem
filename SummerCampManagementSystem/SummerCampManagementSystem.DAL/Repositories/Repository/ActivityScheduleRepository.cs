using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class ActivityScheduleRepository : GenericRepository<ActivitySchedule>, IActivityScheduleRepository
    {
        private new readonly CampEaseDatabaseContext _context;
        public ActivityScheduleRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ActivitySchedule>> GetAllSchedule()
        {
            return await _context.ActivitySchedules
                .Include(s => s.location)
                .Include(s => s.staff)
                .ToListAsync();
        }

        
        public async Task<ActivitySchedule?> GetScheduleById(int id)
        {
            return await _context.ActivitySchedules
                .Include(s => s.location)
                .Include(s => s.staff)
                .FirstOrDefaultAsync(s => s.activityScheduleId == id);
        }
        public async Task<bool> IsTimeOverlapAsync(int? campId, DateTime start, DateTime end, int? excludeScheduleId = null)
        {
            if (campId == null)
                return false;

            return await _context.ActivitySchedules
            .Include(s => s.activity)
            .AnyAsync(s =>
                s.activity.campId == campId &&
               (excludeScheduleId == null || s.activityScheduleId != excludeScheduleId) &&
                s.startTime < end && s.endTime > start);
        }

        public async Task<IEnumerable<ActivitySchedule>> GetOptionalScheduleByCampIdAsync(int campId)
        {
            return await _context.ActivitySchedules
                .Include(s => s.location)
                .Include(s => s.staff)
                .Include(s => s.activity)
                .Where(s => s.activity.campId == campId && s.coreActivityId != null)
                .ToListAsync();
        }

        public async Task<IEnumerable<ActivitySchedule>> GetCoreScheduleByCampIdAsync(int campId)
        {
            return await _context.ActivitySchedules
                .Include(s => s.location)
                .Include(s => s.staff)
                .Include(s => s.activity)
                .Where(s => s.activity.campId == campId && s.coreActivityId == null)
                .ToListAsync();
        }

        public async Task<IEnumerable<ActivitySchedule>> GetScheduleByCampIdAsync(int campId)
        {
            return await _context.ActivitySchedules
                .Include(s => s.location)
                .Include(s => s.staff)
                .Include(s => s.activity)
                .Where(s => s.activity.campId == campId)
                .ToListAsync();
        }

        public async Task<ActivitySchedule?> GetByIdWithActivityAsync(int id)
        {
            return await _context.ActivitySchedules
                .Include(s => s.location)
                .Include (s => s.staff)
                .Include(s => s.activity)
                .FirstOrDefaultAsync(s => s.activityScheduleId == id);
        }

        public async Task<bool> ExistsInSameTimeAndLocationAsync(int locationId, DateTime start, DateTime end, int? excludeScheduleId = null)
        {
            return await _context.ActivitySchedules
                .AnyAsync(s =>
                    s.locationId == locationId &&
                    (excludeScheduleId == null || s.activityScheduleId != excludeScheduleId) &&
                    s.startTime <= end && s.endTime >= start
                );
        }


        public async Task<bool> IsStaffBusyAsync(int staffId, DateTime start, DateTime end, int? excludeScheduleId = null)
        {
            return await _context.ActivitySchedules
                .AnyAsync(s =>
                    s.staffId == staffId &&
                    (excludeScheduleId == null || s.activityScheduleId != excludeScheduleId) &&
                    s.startTime <= end && s.endTime >= start
                );
        }

        public async Task<bool> IsStaffOfActivitySchedule(int staffId, int activityScheduleId)
        {
            return await _context.ActivitySchedules
                .AnyAsync(s =>
                    s.activityScheduleId == activityScheduleId &&
                    s.staffId == staffId
                );
        }

        public async Task<IEnumerable<ActivitySchedule>> GetAllSchedulesByStaffIdAsync(int staffId, int campId)
        {
            return await _context.ActivitySchedules
                .Include(s => s.staff)
                .Include(a => a.activity)
                .Include(a => a.location)
                .Include(a => a.activity.camp) 
                .Where(a => a.staffId == staffId && a.activity.campId == campId)
                .ToListAsync();
        }

        public async Task<IEnumerable<ActivitySchedule>> GetByCampAndStaffAsync(int campId, int staffId)
        {
            return await _context.ActivitySchedules
                .Include(s => s.location)
                .Include(a => a.activity)
                .Include(a => a.staff)
                .Include(a => a.GroupActivities)
                    .ThenInclude(ga => ga.camperGroup)
                .Where(a =>
                    a.activity.campId == campId &&
                    (
                        (a.staffId == staffId && a.coreActivityId != null) ||
                        (a.staffId == staffId && a.activity.activityType == ActivityType.Resting.ToString()) ||
                        a.GroupActivities.Any(ga => ga.camperGroup.supervisorId == staffId)
                    )
                    && a.status.ToLower() == "pendingattendance"
                    )
                .ToListAsync();
        }

        public async Task<IEnumerable<ActivitySchedule>> GetAllWithActivityAndAttendanceAsync(int campId, int camperId)
        {
            return await _context.ActivitySchedules
                .Include(s => s.location)
                .Include(a => a.staff)
                .Include(s => s.activity)
                .Include(s => s.AttendanceLogs.Where(a => a.camperId == camperId))
                .Where(s => s.activity.campId == campId)
                .ToListAsync();
        }

        public async Task<IEnumerable<ActivitySchedule>> GetActivitySchedulesByDateAsync(DateTime fromDate, DateTime toDate)
        {
            return await _context.ActivitySchedules
                .Include(s => s.location)
                .Include(s => s.staff)
                .Include(s => s.activity)
                .Where(s => s.startTime >= fromDate && s.endTime <= toDate)
                .ToListAsync();
        }

        public async Task<ActivitySchedule?> GetOptionalByCoreAsync(int coreActivityId)
        {
            return await _context.ActivitySchedules
                .FirstOrDefaultAsync(a => a.isOptional && a.coreActivityId == coreActivityId);
        }
    }
}
