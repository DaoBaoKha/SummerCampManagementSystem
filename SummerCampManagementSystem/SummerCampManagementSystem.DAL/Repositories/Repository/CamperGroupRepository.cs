using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class CamperGroupRepository : GenericRepository<CamperGroup>, ICamperGroupRepository
    {
        public CamperGroupRepository(CampEaseDatabaseContext context) : base(context)
        {
        }

        /// <summary>
        /// Get all camper IDs for a specific group
        /// </summary>
        public async Task<IEnumerable<int>> GetCamperIdsByGroupIdAsync(int groupId)
        {
            return await _context.Set<CamperGroup>()
                .Where(cg => cg.groupId == groupId)
                .Select(cg => cg.camperId)
                .ToListAsync();
        }

        /// <summary>
        /// Get all group IDs that a specific camper belongs to
        /// </summary>
        public async Task<IEnumerable<int>> GetGroupIdsByCamperIdAsync(int camperId)
        {
            return await _context.Set<CamperGroup>()
                .Where(cg => cg.camperId == camperId)
                .Select(cg => cg.groupId)
                .ToListAsync();
        }

        /// <summary>
        /// Check if a camper is in a specific group
        /// </summary>
        public async Task<bool> IsCamperInGroupAsync(int camperId, int groupId)
        {
            return await _context.Set<CamperGroup>()
                .AnyAsync(cg => cg.camperId == camperId && cg.groupId == groupId);
        }

        /// <summary>
        /// Get all campers with details for a specific group
        /// </summary>
        public async Task<IEnumerable<Camper>> GetCampersByGroupIdAsync(int groupId)
        {
            return await _context.Set<CamperGroup>()
                .Where(cg => cg.groupId == groupId)
                .Include(cg => cg.camper)
                .Select(cg => cg.camper)
                .ToListAsync();
        }
    }
}
