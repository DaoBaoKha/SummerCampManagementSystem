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
    public class ReportRepository : GenericRepository<Report>, IReportRepository
    {
        public ReportRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> IsCamperOfActivityAsync(int camperId, int activityId)
        {
            return await _context.AttendanceLogs
                .AnyAsync(al => al.camperId == camperId && al.activitySchedule.activityId == activityId);
        }

        public async Task<IEnumerable<Report>> GetReportsByCamperAsync(int camperId, int? campId = null)
        {
            var query = _context.Reports
                .Where(r => r.camperId == camperId)
                .Include(r => r.camper)
                .Include(r => r.reportedByNavigation)
                .Include(r => r.activitySchedule)
                    .ThenInclude(a => a.activity)
                .Include(r => r.transportSchedule)
                .AsQueryable();

            if (campId.HasValue)
            {
                query = query.Where(r =>
                    (r.transportSchedule != null && r.transportSchedule.campId == campId.Value) ||
                    (r.activitySchedule != null && r.activitySchedule.activity != null && r.activitySchedule.activity.campId == campId.Value));
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<Report>> GetReportsByStaffAsync(int staffId)
        {
            return await _context.Reports
                .Where(r => r.reportedBy == staffId)
                .Include(r => r.camper)
                .Include(r => r.reportedByNavigation)
                .Include(r => r.activitySchedule)
                .Include(r => r.transportSchedule)
                .OrderByDescending(r => r.createAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Report>> GetReportsByCampAsync(int campId)
        {
            return await _context.Reports
                .Include(r => r.camper)
                .Include(r => r.reportedByNavigation)
                .Include(r => r.activitySchedule)
                    .ThenInclude(a => a.activity)
                .Include(r => r.transportSchedule)
                .Where(r =>
                    (r.transportSchedule != null && r.transportSchedule.campId == campId) ||
                    (r.activitySchedule != null && r.activitySchedule.activity != null && r.activitySchedule.activity.campId == campId))
                .OrderByDescending(r => r.createAt)
                .ToListAsync();
        }
    }
}
