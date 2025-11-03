using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class CamperGroupRepository : GenericRepository<CamperGroup>, ICamperGroupRepository
    {
        public CamperGroupRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> isSupervisor(int staffId)
        {
            return await _context.CamperGroups
                .AnyAsync(a =>
                    a.supervisorId == staffId);
        }

        public async Task<IEnumerable<CamperGroup>> GetByCampIdAsync(int campId)
        {
            return await _context.CamperGroups
                .Include(g => g.Campers)
                .Where(g => g.campId == campId)
                .ToListAsync();
        }
    }
}
