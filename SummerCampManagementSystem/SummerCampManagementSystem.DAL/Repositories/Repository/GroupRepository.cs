using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class GroupRepository : GenericRepository<Group>, IGroupRepository
    {
        private readonly ILogger<GroupRepository> _logger;

        public GroupRepository(CampEaseDatabaseContext context, ILogger<GroupRepository> logger) : base(context)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Group>> GetAllCamperGroups()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("[GroupRepository] GetAllCamperGroups called - MemoryBefore={MemoryMB}MB",
                GC.GetTotalMemory(false) / 1024 / 1024);

            try
            {
                // only return active groups
                var groups = await _context.Groups
                    .AsNoTracking()
                    .Where(g => g.status == "Active")
                    .Include(g => g.supervisor)
                    .AsSplitQuery() // prevent Cartesian explosion
                    .ToListAsync();

                stopwatch.Stop();
                _logger.LogInformation(
                    "[GroupRepository] GetAllCamperGroups completed - Count={Count}, ElapsedMs={ElapsedMs}, MemoryAfter={MemoryMB}MB",
                    groups.Count(), stopwatch.ElapsedMilliseconds, GC.GetTotalMemory(false) / 1024 / 1024);

                return groups;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[GroupRepository] ERROR in GetAllCamperGroups - ElapsedMs={ElapsedMs}, Error={ErrorMessage}, StackTrace={StackTrace}",
                    stopwatch.ElapsedMilliseconds, ex.Message, ex.StackTrace);
                throw;
            }
        }

        public async Task<Group?> GetCamperGroupById(int id)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("[GroupRepository] GetCamperGroupById called - GroupId={GroupId}", id);

            try
            {
                // no status filter - allow viewing inactive groups by ID
                var group = await _context.Groups
                    .AsNoTracking()
                    .Include(g => g.supervisor)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(g => g.groupId == id);

                stopwatch.Stop();
                _logger.LogInformation(
                    "[GroupRepository] GetCamperGroupById completed - GroupId={GroupId}, Found={Found}, ElapsedMs={ElapsedMs}",
                    id, group != null, stopwatch.ElapsedMilliseconds);

                return group;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[GroupRepository] ERROR in GetCamperGroupById - GroupId={GroupId}, ElapsedMs={ElapsedMs}, Error={ErrorMessage}, StackTrace={StackTrace}",
                    id, stopwatch.ElapsedMilliseconds, ex.Message, ex.StackTrace);
                throw;
            }
        }
        public async Task<bool> isSupervisor(int staffId, int campId)
        {
            _logger.LogInformation("[GroupRepository] isSupervisor called - StaffId={StaffId}, CampId={CampId}", staffId, campId);

            try
            {
                var result = await _context.Groups
                    .AsNoTracking()
                    .AnyAsync(a =>
                        a.supervisorId == staffId &&
                        a.campId == campId &&
                        a.status == "Active");  // check only active groups

                _logger.LogInformation("[GroupRepository] isSupervisor result - StaffId={StaffId}, CampId={CampId}, IsSupervisor={Result}",
                    staffId, campId, result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[GroupRepository] ERROR in isSupervisor - StaffId={StaffId}, CampId={CampId}, Error={ErrorMessage}",
                    staffId, campId, ex.Message);
                throw;
            }
        }

        public async Task<IEnumerable<Group>> GetByCampIdAsync(int campId)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("[GroupRepository] GetByCampIdAsync called - CampId={CampId}", campId);

            try
            {
                // only return active groups
                var groups = await _context.Groups
                    .AsNoTracking()
                    .Where(g => g.campId == campId && g.status == "Active")
                    .Include(g => g.supervisor)
                    .AsSplitQuery()
                    .ToListAsync();

                stopwatch.Stop();
                _logger.LogInformation(
                    "[GroupRepository] GetByCampIdAsync completed - CampId={CampId}, Count={Count}, ElapsedMs={ElapsedMs}",
                    campId, groups.Count(), stopwatch.ElapsedMilliseconds);

                return groups;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[GroupRepository] ERROR in GetByCampIdAsync - CampId={CampId}, ElapsedMs={ElapsedMs}, Error={ErrorMessage}, StackTrace={StackTrace}",
                    campId, stopwatch.ElapsedMilliseconds, ex.Message, ex.StackTrace);
                throw;
            }
        }

        public async Task<Group?> GetGroupBySupervisorIdAsync(int supervisorId, int campId)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var memoryBefore = GC.GetTotalMemory(false) / 1024 / 1024;
            
            _logger.LogInformation(
                "[GroupRepository] GetGroupBySupervisorIdAsync called - SupervisorId={SupervisorId}, CampId={CampId}, MemoryBefore={MemoryMB}MB",
                supervisorId, campId, memoryBefore);

            try
            {
                // only return active groups
                var group = await _context.Groups
                    .AsNoTracking()
                    .Where(g => g.supervisorId == supervisorId && g.campId == campId && g.status == "Active")
                    .Include(g => g.supervisor)
                    .Include(g => g.camp)
                    .AsSplitQuery() // prevents Cartesian explosion warning
                    .FirstOrDefaultAsync();

                stopwatch.Stop();
                var memoryAfter = GC.GetTotalMemory(false) / 1024 / 1024;
                
                _logger.LogInformation(
                    "[GroupRepository] GetGroupBySupervisorIdAsync completed - SupervisorId={SupervisorId}, CampId={CampId}, Found={Found}, ElapsedMs={ElapsedMs}, MemoryBefore={MemoryBeforeMB}MB, MemoryAfter={MemoryAfterMB}MB, MemoryDelta={MemoryDeltaMB}MB",
                    supervisorId, campId, group != null, stopwatch.ElapsedMilliseconds, memoryBefore, memoryAfter, memoryAfter - memoryBefore);

                return group;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var memoryAfter = GC.GetTotalMemory(false) / 1024 / 1024;
                
                _logger.LogError(ex,
                    "[GroupRepository] CRITICAL ERROR in GetGroupBySupervisorIdAsync - SupervisorId={SupervisorId}, CampId={CampId}, ElapsedMs={ElapsedMs}, MemoryBefore={MemoryBeforeMB}MB, MemoryAfter={MemoryAfterMB}MB, Error={ErrorMessage}, ExceptionType={ExceptionType}, StackTrace={StackTrace}",
                    supervisorId, campId, stopwatch.ElapsedMilliseconds, memoryBefore, memoryAfter, ex.Message, ex.GetType().Name, ex.StackTrace);
                throw;
            }
        }

        public async Task<IEnumerable<Group>> GetGroupsByActivityScheduleIdAsync(int activityScheduleId)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("[GroupRepository] GetGroupsByActivityScheduleIdAsync called - ActivityScheduleId={ActivityScheduleId}",
                activityScheduleId);

            try
            {
                // use projection to avoid circular references and reduce memory footprint
                // only return active groups
                var groups = await _context.GroupActivities
                    .AsNoTracking()
                    .Where(ga => ga.activityScheduleId == activityScheduleId && ga.group.status == "Active")
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
                        status = ga.group.status,
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

                stopwatch.Stop();
                _logger.LogInformation(
                    "[GroupRepository] GetGroupsByActivityScheduleIdAsync completed - ActivityScheduleId={ActivityScheduleId}, Count={Count}, ElapsedMs={ElapsedMs}",
                    activityScheduleId, groups.Count(), stopwatch.ElapsedMilliseconds);

                return groups;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[GroupRepository] ERROR in GetGroupsByActivityScheduleIdAsync - ActivityScheduleId={ActivityScheduleId}, ElapsedMs={ElapsedMs}, Error={ErrorMessage}, StackTrace={StackTrace}",
                    activityScheduleId, stopwatch.ElapsedMilliseconds, ex.Message, ex.StackTrace);
                throw;
            }
        }

        public async Task<List<int?>> GetGroupIdsWithSchedulesAsync(int campId)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("[GroupRepository] GetGroupIdsWithSchedulesAsync called - CampId={CampId}", campId);

            try
            {
                // join GroupActivity -> ActivitySchedule -> Activity -> Check CampId
                var groupIds = await _context.GroupActivities
                    .AsNoTracking()
                    .Include(ga => ga.activitySchedule).ThenInclude(s => s.activity)
                    .AsSplitQuery()
                    .Where(ga => ga.activitySchedule.activity.campId == campId)
                    .Select(ga => ga.groupId)
                    .Distinct()
                    .ToListAsync();

                stopwatch.Stop();
                _logger.LogInformation(
                    "[GroupRepository] GetGroupIdsWithSchedulesAsync completed - CampId={CampId}, Count={Count}, ElapsedMs={ElapsedMs}",
                    campId, groupIds.Count, stopwatch.ElapsedMilliseconds);

                return groupIds;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[GroupRepository] ERROR in GetGroupIdsWithSchedulesAsync - CampId={CampId}, ElapsedMs={ElapsedMs}, Error={ErrorMessage}",
                    campId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }

        public async Task<Group?> GetGroupByCamperAndCamp(int camperId, int campId)
        {
            _logger.LogInformation("[GroupRepository] GetGroupByCamperAndCamp called - CamperId={CamperId}, CampId={CampId}",
                camperId, campId);

            try
            {
                var group = await _context.CamperGroups
                    .AsNoTracking()
                    .Where(c => c.camperId == camperId && c.group.campId == campId)
                    .Select(c => c.group)
                    .FirstOrDefaultAsync();

                _logger.LogInformation("[GroupRepository] GetGroupByCamperAndCamp result - CamperId={CamperId}, CampId={CampId}, Found={Found}",
                    camperId, campId, group != null);

                return group;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[GroupRepository] ERROR in GetGroupByCamperAndCamp - CamperId={CamperId}, CampId={CampId}, Error={ErrorMessage}",
                    camperId, campId, ex.Message);
                throw;
            }
        }

        public async Task<List<(string Name, int Current, int Max)>> GetCapacityAlertsByCampAsync(int campId)
        {
            _logger.LogInformation("[GroupRepository] GetCapacityAlertsByCampAsync called - CampId={CampId}", campId);

            try
            {
                // only return active groups
                var groups = await _context.Groups
                    .AsNoTracking()
                    .Where(g => g.campId == campId && g.status == "Active" && g.maxSize.HasValue && g.maxSize > 0)
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

                _logger.LogInformation(
                    "[GroupRepository] GetCapacityAlertsByCampAsync completed - CampId={CampId}, TotalGroups={TotalGroups}, AlertCount={AlertCount}",
                    campId, groups.Count, alerts.Count);

                return alerts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[GroupRepository] ERROR in GetCapacityAlertsByCampAsync - CampId={CampId}, Error={ErrorMessage}",
                    campId, ex.Message);
                throw;
            }
        }

        public async Task<IEnumerable<Group>> GetGroupsWithCampersByCampIdAsync(int campId)
        {
            // use AsNoTracking for read-only queries
            // only return active groups
            return await _context.Groups
                .AsNoTracking()
                .Include(g => g.CamperGroups)
                .Where(g => g.campId == campId && g.status == "Active")
                .ToListAsync();
        }

        public async Task<IEnumerable<Group>> GetGroupsWithCampersByIdsAsync(List<int> groupIds)
        {
            // use AsNoTracking for read-only queries
            return await _context.Groups
                .AsNoTracking()
                .Include(g => g.CamperGroups)
                .Where(g => groupIds.Contains(g.groupId) && g.status == "Active")
                .ToListAsync();
        }

        public async Task<Group?> GetByIdWithCampAsync(int groupId)
        {
            return await _context.Groups
                .AsNoTracking()
                .Include(g => g.camp)
                .FirstOrDefaultAsync(g => g.groupId == groupId);
        }

        public async Task<Group?> GetByIdWithCamperGroupsAndCampAsync(int groupId)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var memoryBefore = GC.GetTotalMemory(false) / 1024 / 1024;
            
            _logger.LogInformation(
                "[GroupRepository] GetByIdWithCamperGroupsAndCampAsync called - GroupId={GroupId}, MemoryBefore={MemoryMB}MB",
                groupId, memoryBefore);

            try
            {
                // use AsNoTracking for read-only queries
                // use AsSplitQuery to prevent Cartesian explosion with multiple collections
                // no status filter - allow viewing inactive groups by ID
                var group = await _context.Groups
                    .AsNoTracking()
                    .Include(g => g.CamperGroups)
                    .Include(g => g.camp)
                    .Include(g => g.supervisor)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(g => g.groupId == groupId);

                stopwatch.Stop();
                var memoryAfter = GC.GetTotalMemory(false) / 1024 / 1024;
                
                _logger.LogInformation(
                    "[GroupRepository] GetByIdWithCamperGroupsAndCampAsync completed - GroupId={GroupId}, Found={Found}, CamperGroupCount={CamperGroupCount}, ElapsedMs={ElapsedMs}, MemoryBefore={MemoryBeforeMB}MB, MemoryAfter={MemoryAfterMB}MB, MemoryDelta={MemoryDeltaMB}MB",
                    groupId, group != null, group?.CamperGroups?.Count ?? 0, stopwatch.ElapsedMilliseconds, memoryBefore, memoryAfter, memoryAfter - memoryBefore);

                return group;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var memoryAfter = GC.GetTotalMemory(false) / 1024 / 1024;
                
                _logger.LogError(ex,
                    "[GroupRepository] ERROR in GetByIdWithCamperGroupsAndCampAsync - GroupId={GroupId}, ElapsedMs={ElapsedMs}, MemoryBefore={MemoryBeforeMB}MB, MemoryAfter={MemoryAfterMB}MB, Error={ErrorMessage}, StackTrace={StackTrace}",
                    groupId, stopwatch.ElapsedMilliseconds, memoryBefore, memoryAfter, ex.Message, ex.StackTrace);
                throw;
            }
        }
    }
}
