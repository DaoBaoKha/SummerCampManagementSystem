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
    public class GroupActivityRepository : GenericRepository<GroupActivity>, IGroupActivityRepository
    {
        public GroupActivityRepository(CampEaseDatabaseContext context) : base(context)
        { 
            _context = context;
        }

        public async Task<IEnumerable<GroupActivity>> GetByGroupId(int groupId)
        {
            return await _context.GroupActivities
                .Where(ga => ga.groupId == groupId)
                .ToListAsync();
        }

        public async Task<GroupActivity?> GetByGroupAndActivityScheduleId(int groupId, int activityScheduleId)
        {
            return await _context.GroupActivities
                .Where(ga => ga.activityScheduleId == activityScheduleId && ga.groupId == groupId)
                .FirstOrDefaultAsync();
        }

        public async Task<GroupActivity?> GetByIdWithGroupAndCampAsync(int groupActivityId)
        {
            return await _context.GroupActivities
                .AsNoTracking()
                .Include(ga => ga.group)
                    .ThenInclude(g => g.camp)
                .FirstOrDefaultAsync(ga => ga.groupActivityId == groupActivityId);
        }
    }
}
