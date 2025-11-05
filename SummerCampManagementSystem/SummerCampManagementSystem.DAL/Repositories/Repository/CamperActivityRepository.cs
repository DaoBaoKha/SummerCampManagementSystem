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
    public class CamperActivityRepository : GenericRepository<CamperActivity>, ICamperActivityRepository
    {
        private readonly CampEaseDatabaseContext _context;

        public CamperActivityRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }



        public new async Task<IEnumerable<CamperActivity>> GetAllAsync()
        {
            return await _context.CamperActivities
                 .Include(ca => ca.camper)
                 .Include(ca => ca.activitySchedule)
                 .ToListAsync();
        }

        public async Task<int> CamperofOptionalActivityCount(int activityScheduleId)
        {
            return await _context.CamperActivities
                .CountAsync(ca => ca.activityScheduleId == activityScheduleId);
        }

        public new async Task<CamperActivity?> GetByIdAsync(int id)
        {
            return await _context.CamperActivities
                .Include(ca => ca.camper)
                .Include(ca => ca.activitySchedule)
                .FirstOrDefaultAsync(g => g.camperActivityId == id);
        }

        public async Task<bool> IsApprovedAsync(int camperId, int activityId)
        {
            return await _context.CamperActivities
                .AnyAsync(ca => ca.camperId == camperId && ca.activityScheduleId == activityId);
        }
    }
}
