using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class AttendanceLogRepository : GenericRepository<AttendanceLog>, IAttendanceLogRepository
    {
        public AttendanceLogRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> IsCoreScheduleOfCamper(int activityScheduleId, int camperGroupId)
        {
            return await _context.GroupActivities
                .AnyAsync(ga =>
                    ga.activityScheduleId == activityScheduleId &&
                    ga.groupId == camperGroupId);
        }

        public async Task<bool> IsOptionalScheduleOfCamper(int activityScheduleId, int camperId)
        {
            return await _context.CamperActivities
                .AnyAsync(ga =>
                    ga.activityScheduleId == activityScheduleId &&
                    ga.camperId == camperId);
        }

        public async Task<IEnumerable<AttendanceLog>> GetAttendanceLogsByScheduleId(int activityScheduleId)
        {
            return await _context.AttendanceLogs
                .Where(al => al.activityScheduleId == activityScheduleId)
                .ToListAsync();
        }

        public async Task<IEnumerable<ActivitySchedule>> GetAttendedActivitiesByCamperId(int camperId)
        {
            return await _context.ActivitySchedules
                .Where(asch => _context.AttendanceLogs
                    .Any(al => al.camperId == camperId && al.activityScheduleId == asch.activityScheduleId && al.participantStatus == "Present"))
                .Include(asch => asch.activity)
                .Include(asch => asch.location)
                .Include(asch => asch.staff)
                .ToListAsync();
        }

        public async Task<IEnumerable<Camper>> GetAttendedCampersByActivityScheduleId(int activityScheduleId)
        {
            return await _context.AttendanceLogs
                .Where(al => al.activityScheduleId == activityScheduleId && al.participantStatus == "Present")
                .Select(al => al.camper)
                .ToListAsync();
        }
    }
}
