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

        public async Task<List<int?>> GetGroupIdsWithSchedulesAsync(int campId)
        {
            // Logic: Join GroupActivity -> ActivitySchedule -> Activity -> Check CampId
            return await _context.GroupActivities
                .Include(ga => ga.activitySchedule).ThenInclude(s => s.activity)
                .Where(ga => ga.activitySchedule.activity.campId == campId)
                .Select(ga => ga.groupId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<Group?> GetGroupByCamperAndCamp(int camperId, int campId)
        {
            return await _context.CamperGroups
                .Where(c => c.camperId == camperId && c.group.campId == campId)
                .Select(c => c.group)
                .FirstOrDefaultAsync();
        }

        public async Task<List<(string Name, int Current, int Max)>> GetCapacityAlertsByCampAsync(int campId)
        {
            var groups = await _context.Groups
                .Where(g => g.campId == campId && g.maxSize.HasValue && g.maxSize > 0)
                .Select(g => new
                {
                    g.groupName,
                    Current = g.currentSize ?? 0,
                    Max = g.maxSize.Value
                })
                .ToListAsync();

            // filter groups exceeding 80% capacity
            var alerts = groups
                .Where(g => (double)g.Current / g.Max >= 0.8)
                .Select(g => (g.groupName ?? "Unknown", g.Current, g.Max))
                .ToList();

            return alerts;
        }
    }
}
