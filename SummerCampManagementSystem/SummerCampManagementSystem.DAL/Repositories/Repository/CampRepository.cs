using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class CampRepository : GenericRepository<Camp>, ICampRepository
    {
        public CampRepository(CampEaseDatabaseContext context) : base(context)
        {
        }
        public async Task<IEnumerable<Camp>> GetCampsByTypeAsync(int campTypeId)
        {
            return await _context.Camps.Where(c => c.campTypeId == campTypeId).ToListAsync();
        }

        public async Task<IEnumerable<Camp>> GetCampsByStaffIdAsync(int staffId)
        {
            var activityCampIds = await _context.ActivitySchedules
                .Where(a => a.staffId == staffId)
                .Select(a => a.activity.campId)
                .ToListAsync();

            var groupCampIds = await _context.Groups
                .Where(g => g.supervisorId == staffId) 
                .Select(g => g.campId)
                .ToListAsync();

            var accommodationCampIds = await _context.Accommodations
                .Where(a => a.supervisorId == staffId)
                .Select(a => a.campId)
                .ToListAsync();

            var allCampIds = activityCampIds
                .Concat(groupCampIds)
                .Concat(accommodationCampIds)
                .Distinct()
                .ToList();

            var allCamps = await _context.Camps
                .Where(c => allCampIds.Contains(c.campId))
                .ToListAsync();

            return allCamps;
        }

    }
}
