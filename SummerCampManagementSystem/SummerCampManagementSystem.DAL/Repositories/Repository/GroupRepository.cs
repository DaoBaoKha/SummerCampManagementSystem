using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class GroupRepository : GenericRepository<Group>, IGroupRepository
    {
        public GroupRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Group>> GetAllCamperGroups()
        {
            return await _context.Groups
                .Include(g => g.supervisor)
                //.Include(g => g.Campers)
                .ToListAsync();
        }

        public async Task<Group?> GetCamperGroupById(int id)
        {
            return await _context.Groups
                .Include(g => g.supervisor)
                .FirstOrDefaultAsync(g => g.groupId == id);
        }
        public async Task<bool> isSupervisor(int staffId, int campId)
        {
            return await _context.Groups
                .AnyAsync(a =>
                    a.supervisorId == staffId &&
                    a.campId == campId);
        }

        public async Task<IEnumerable<Group>> GetByCampIdAsync(int campId)
        {
            return await _context.Groups
                .Include(g => g.supervisor)
                //.Include(g => g.Campers)
                .Where(g => g.campId == campId)
                .ToListAsync();
        }

        public async Task<Group?> GetGroupBySupervisorIdAsync(int supervisorId, int campId)
        {
            return await _context.Groups
                .Include(g => g.supervisor)
                .Include(g => g.camp)
                .FirstOrDefaultAsync(g => g.supervisorId == supervisorId && g.campId == campId);
        }

        public async Task<IEnumerable<Group>> GetGroupsByActivityScheduleIdAsync(int activityScheduleId)
        {
            return await _context.GroupActivities
                .Include(ga => ga.group.supervisor)
                .Include(ga => ga.group)
                .Where(g => g.activityScheduleId == activityScheduleId)
                .Select(ga => ga.group)
                .ToListAsync();
        }
    }
}
