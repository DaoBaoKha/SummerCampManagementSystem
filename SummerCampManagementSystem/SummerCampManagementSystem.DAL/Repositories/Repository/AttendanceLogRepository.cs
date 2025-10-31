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
                    ga.camperGroupId == camperGroupId);
        }

        public async Task<bool> IsOptionalScheduleOfCamper(int activityScheduleId, int camperId)
        {
            return await _context.CamperActivities
                .AnyAsync(ga =>
                    ga.activityScheduleId == activityScheduleId &&
                    ga.camperId == camperId);
        }

    }
}
