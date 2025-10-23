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
                !s.isOptional &&
                s.startTime < end &&
                s.endTime > start);
        }

        public async Task<ActivitySchedule?> GetByIdWithActivityAsync(int id)
        {
            return await _context.ActivitySchedules
                .Include(s => s.activity)
                .FirstOrDefaultAsync(s => s.activityScheduleId == id);
        }

    }
}
