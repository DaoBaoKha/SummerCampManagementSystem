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
                .Include(s => s.activity)
                .Include(s => s.location)
                .Include(s => s.staff)
                .Include(s => s.livestream)
                .ToListAsync();
        }

        public IQueryable<ActivitySchedule> GetQueryableWithBaseIncludes()
        {
            return _context.ActivitySchedules
                .Include(s => s.location)
                .Include(s => s.staff)
                .Include(s => s.activity)
                .Include(s => s.livestream)
                .AsNoTracking(); // Tối ưu hóa bộ nhớ cho các truy vấn chỉ đọc
        }


        public async Task<ActivitySchedule?> GetScheduleById(int id)
        {
            return await _context.ActivitySchedules
                .Include(s => s.location)
                .Include(s => s.activity)
                .Include(s => s.staff)
                .Include(s => s.livestream)
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
                .Include(s => s.livestream)
                .Where(s => s.activity.campId == campId && s.activity.activityType == ActivityType.Optional.ToString())
                .ToListAsync();
        }

        public async Task<IEnumerable<ActivitySchedule>> GetCoreScheduleByCampIdAsync(int campId)
        {
            return await _context.ActivitySchedules
                .Include(s => s.location)
                .Include(s => s.staff)
                .Include(s => s.activity)
                .Include(s => s.livestream)
                .Where(s => s.activity.campId == campId && s.activity.activityType == ActivityType.Core.ToString())
                .ToListAsync();
        }

        public async Task<IEnumerable<ActivitySchedule>> GetScheduleByCampIdAsync(int campId)
        {
            return await _context.ActivitySchedules
                .Include(s => s.location)
                .Include(s => s.staff)
                .Include(s => s.activity)
                .Include(s => s.livestream)
                .Where(s => s.activity.campId == campId)
                .ToListAsync();
        }

        public async Task<ActivitySchedule?> GetByIdWithActivityAsync(int id)
        {
            return await _context.ActivitySchedules
                .Include(s => s.location)
                .Include(s => s.staff)
                .Include(s => s.activity)
                .Include(s => s.livestream)
                .FirstOrDefaultAsync(s => s.activityScheduleId == id);
        }

        public async Task<bool> ExistsInSameTimeAndLocationAsync(int locationId, DateTime start, DateTime end, int? excludeScheduleId = null)
        {
            return await _context.ActivitySchedules
                .AnyAsync(s =>
                    s.locationId == locationId &&
                    (excludeScheduleId == null || s.activityScheduleId != excludeScheduleId) &&
                    s.startTime < end && s.endTime > start
                );
        }


        public async Task<bool> IsStaffBusyAsync(int staffId, DateTime start, DateTime end, int? excludeScheduleId = null)
        {
            return await _context.ActivitySchedules
                .AnyAsync(s =>
                    s.staffId == staffId &&
                    (excludeScheduleId == null || s.activityScheduleId != excludeScheduleId) &&
                    s.startTime < end && s.endTime > start
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
                .Where(a => a.staffId == staffId && a.activity.campId == campId)
                .Include(s => s.staff)
                .Include(s => s.activity)
                .Include(s => s.location)
                .Include(s => s.livestream)
                .Include(s => s.activity.camp)
                .ToListAsync();
        }

        public async Task<IEnumerable<ActivitySchedule>> GetByCampAndStaffAsync(int campId, int staffId)
        {
            return await _context.ActivitySchedules
                .Include(s => s.location)
                .Include(a => a.activity)
                .Include(a => a.staff)
                .Include(s => s.livestream)
                .Include(a => a.GroupActivities)
                    .ThenInclude(ga => ga.group)
                .Where(a =>
                    a.activity.campId == campId &&
                    (
                        (a.staffId == staffId && a.activity.activityType == ActivityType.Optional.ToString()) ||
                        (a.AccommodationActivitySchedules.Any(aa => aa.accommodation.supervisorId == staffId)) ||
                        (a.GroupActivities.Any(ga => ga.group.supervisorId == staffId)
                            && a.activity.activityType != ActivityType.Checkin.ToString()
                            && a.activity.activityType != ActivityType.Checkout.ToString()
                        )
                    )
                    && (a.status.ToLower() == "pendingattendance" || a.status.ToLower() == "attendancechecked")
                    )
                .ToListAsync();
        }

        public async Task<IEnumerable<ActivitySchedule>> GetCheckInCheckoutByCampAndStaffAsync(int campId, int staffId)
        {
            return await _context.ActivitySchedules
                .Include(s => s.location)
                .Include(a => a.activity)
                .Include(a => a.staff)
                .Include(s => s.livestream)
                .Include(a => a.GroupActivities)
                    .ThenInclude(ga => ga.group)
                .Where(a =>
                    a.activity.campId == campId
                    && a.GroupActivities.Any(ga => ga.group.supervisorId == staffId)
                    &&
                    ( a.activity.activityType == ActivityType.Checkin.ToString() || a.activity.activityType == ActivityType.Checkout.ToString())
                    && a.status.ToLower() == "pendingattendance"
                    )
                .ToListAsync();
        }

        public async Task<IEnumerable<ActivitySchedule>> GetSchedulesByGroupStaffAsync(int campId, int staffId)
        {
            return await _context.ActivitySchedules
                .Include(s => s.location)
                .Include(a => a.activity)
                .Include(a => a.staff)
                .Include(s => s.livestream)
                .Include(a => a.GroupActivities)
                    .ThenInclude(ga => ga.group)
                .Where(a => a.activity.campId == campId
                             && a.GroupActivities.Any(ga => ga.group.supervisorId == staffId))
                 .ToListAsync();
        }


        public async Task<IEnumerable<ActivitySchedule>> GetAllTypeSchedulesByStaffAsync(int campId, int staffId)
        {
            return await _context.ActivitySchedules
                .Include(s => s.location)
                .Include(a => a.activity)
                .Include(a => a.staff)
                .Include(s => s.livestream)
                .Include(a => a.GroupActivities)
                    .ThenInclude(ga => ga.group)
                .Include(a => a.AccommodationActivitySchedules)
                .Where(a =>
                        a.activity.campId == campId &&
                        (
                        a.staffId == staffId
                        || a.AccommodationActivitySchedules.Any(aa => aa.accommodation.supervisorId == staffId)
                        || a.GroupActivities.Any(ga => ga.group.supervisorId == staffId)  
                        )
                      )
                 .ToListAsync();
        }
        public async Task<IEnumerable<ActivitySchedule>> GetAllWithActivityAndAttendanceAsync(int campId, int camperId)
        {
            return await _context.ActivitySchedules
                .Include(s => s.location)
                .Include(a => a.staff)
                .Include(s => s.activity)
                .Include(s => s.livestream)
                .Include(s => s.AttendanceLogs.Where(a => a.camperId == camperId))
                .Where(s => s.activity.campId == campId && s.coreActivityId == null)
                .ToListAsync();
        }

        public async Task<IEnumerable<ActivitySchedule>> GetOptionalSchedulesByCamperAsync(int camperId)
        {
            var optionalIds = await _context.CamperActivities
                .Where(ca => ca.camperId == camperId)
                .Select(ca => ca.activityScheduleId)
                .ToListAsync();

            return await _context.ActivitySchedules
                .Include(s => s.location)
                .Include(s => s.staff)
                .Include(s => s.activity)
                .Include(s => s.livestream)
                .Include(s => s.AttendanceLogs.Where(a => a.camperId == camperId))
                .Where(s => optionalIds.Contains(s.activityScheduleId))
                .ToListAsync();
        }

        public async Task<IEnumerable<ActivitySchedule>> GetActivitySchedulesByDateAsync(DateTime fromDate, DateTime toDate)
        {
            return await _context.ActivitySchedules
                .Include(s => s.location)
                .Include(s => s.staff)
                .Include(s => s.activity)
                .Include(s => s.livestream)
                .Where(s => s.startTime >= fromDate && s.endTime <= toDate)
                .ToListAsync();
        }

        public async Task<ActivitySchedule?> GetOptionalByCoreAsync(int coreActivityId)
        {
            return await _context.ActivitySchedules
                .FirstOrDefaultAsync(a => a.isOptional == true && a.coreActivityId == coreActivityId);
        }
        
        public async Task<bool> IsCamperofCamp(int campId, int camperId)
        {
            return await _context.RegistrationCampers
                .AnyAsync(rc =>
                    rc.registration.campId == campId &&
                    rc.camperId == camperId
                );
        }

        public async Task<IEnumerable<int>> GetBusyStaffIdsInActivityAsync(DateTime startUtc, DateTime endUtc)
        {
            // query to get all staff with same activity schedule
            return await _context.ActivitySchedules
                .Where(s => s.staffId.HasValue &&
                            s.startTime <= endUtc && s.endTime >= startUtc)
                .Select(s => s.staffId.Value)
                .Distinct()
                .ToListAsync();
        }
    }
}
