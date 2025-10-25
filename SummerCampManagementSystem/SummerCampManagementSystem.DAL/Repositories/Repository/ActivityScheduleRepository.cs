using Microsoft.EntityFrameworkCore;
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

        public async Task<bool> IsTimeOverlapAsync(int? campId, DateTime start, DateTime end)
        {
            if (campId == null)
                return false;

            return await _context.ActivitySchedules
            .Include(s => s.activity)
            .AnyAsync(s =>
                s.activity.campId == campId &&
                s.startTime < end &&
                s.endTime > start);
        }

        public async Task<ActivitySchedule?> GetByIdWithActivityAsync(int id)
        {
            return await _context.ActivitySchedules
                .Include(s => s.activity)
                .FirstOrDefaultAsync(s => s.activityScheduleId == id);
        }

        public async Task<bool> ExistsInSameTimeAndLocationAsync(int locationId, DateTime start, DateTime end)
        {
            return await _context.ActivitySchedules.AnyAsync(s =>
                s.locationId == locationId &&
                ((start < s.endTime) && (end > s.startTime)));
        }

        public async Task<bool> IsStaffBusyAsync(int staffId, DateTime start, DateTime end)
        {
            return await _context.ActivitySchedules
                .AnyAsync(a =>
                    a.staffId == staffId && 
                    a.startTime < end &&
                    a.endTime > start);
        }

        public async Task<IEnumerable<ActivitySchedule>> GetByCampAndStaffAsync(int campId, int staffId)
        {
            return await _context.ActivitySchedules
                .Include(a => a.activity)
                .Include(a => a.GroupActivities)
                    .ThenInclude(ga => ga.camperGroup)
                .Where(a =>
                    a.activity.campId == campId &&
                    (
                        a.staffId == staffId || // Staff được gán trực tiếp
                        a.GroupActivities.Any(ga => ga.camperGroup.supervisorId == staffId) // Supervisor của nhóm
                    ))
                .ToListAsync();
        }

        public async Task<IEnumerable<ActivitySchedule>> GetAllWithActivityAndAttendanceAsync(int campId, int camperId)
        {
            return await _context.ActivitySchedules
                .Include(s => s.activity)
                .Include(s => s.AttendanceLogs.Where(a => a.camperId == camperId))
                .Where(s => s.activity.campId == campId)
                .ToListAsync();
        }

    }
}
