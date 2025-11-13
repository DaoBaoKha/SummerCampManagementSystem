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

        public async Task<IEnumerable<CamperGroup>> GetAllCamperGroups()
        {
            return await _context.CamperGroups
                .Include(g => g.supervisor)
                .Include(g => g.Campers)
                .ToListAsync();
        }

        public async Task<CamperGroup?> GetCamperGroupById(int id)
        {
            return await _context.CamperGroups
                .Include(g => g.supervisor)
                .FirstOrDefaultAsync(g => g.camperGroupId == id);
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
                .Include(g => g.supervisor)
                .Include(g => g.Campers)
                .Where(g => g.campId == campId)
                .ToListAsync();
        }

        public async Task<CamperGroup?> GetGroupBySupervisorIdAsync(int supervisorId, int campId)
        {
            return await _context.CamperGroups
                .Include(g => g.supervisor)
                .Include(g => g.camp)
                .FirstOrDefaultAsync(g => g.supervisorId == supervisorId && g.campId == campId);
        }

        public async Task<IEnumerable<CamperGroup>> GetGroupsByActivityScheduleIdAsync(int activityScheduleId)
        {
            return await _context.GroupActivities
                .Include(ga => ga.camperGroup.supervisor)
                .Include(ga => ga.camperGroup)
                .Where(g => g.activityScheduleId == activityScheduleId)
                .Select(ga => ga.camperGroup)
                .ToListAsync();
        }
    }
}
