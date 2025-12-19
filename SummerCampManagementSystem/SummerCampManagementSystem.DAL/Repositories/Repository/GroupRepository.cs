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
                .AsNoTracking()
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
                .AsNoTracking()
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
            // use projection to avoid circular references and reduce memory footprint
            return await _context.GroupActivities
                .AsNoTracking()
                .Where(ga => ga.activityScheduleId == activityScheduleId)
                .Select(ga => new Group
                {
                    groupId = ga.group.groupId,
                    groupName = ga.group.groupName,
                    description = ga.group.description,
                    currentSize = ga.group.currentSize,
                    maxSize = ga.group.maxSize,
                    supervisorId = ga.group.supervisorId,
                    campId = ga.group.campId,
                    minAge = ga.group.minAge,
                    maxAge = ga.group.maxAge,
                    supervisor = ga.group.supervisor != null ? new UserAccount
                    {
                        userId = ga.group.supervisor.userId,
                        firstName = ga.group.supervisor.firstName,
                        lastName = ga.group.supervisor.lastName,
                        email = ga.group.supervisor.email,
                        role = ga.group.supervisor.role
                    } : null
                })
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

<<<<<<< HEAD
        public async Task<IEnumerable<Group>> GetGroupsWithCampersByCampIdAsync(int campId)
        {
            return await _context.Groups
                .Include(g => g.CamperGroups)
                .Where(g => g.campId == campId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Group>> GetGroupsWithCampersByIdsAsync(List<int> groupIds)
        {
            return await _context.Groups
                .Include(g => g.CamperGroups)
                .Where(g => groupIds.Contains(g.groupId))
                .ToListAsync();
=======
        public async Task<Group?> GetByIdWithCamperGroupsAndCampAsync(int groupId)
        {
            return await _context.Groups
                .Include(g => g.CamperGroups)
                .Include(g => g.camp)
                .FirstOrDefaultAsync(g => g.groupId == groupId);
>>>>>>> aa1f3938992fc860e41f971b323f4af9d35c90c1
        }
    }
}
