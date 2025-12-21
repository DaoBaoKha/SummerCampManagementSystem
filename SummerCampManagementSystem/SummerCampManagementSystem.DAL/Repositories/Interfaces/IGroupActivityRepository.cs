using SummerCampManagementSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IGroupActivityRepository : IGenericRepository<GroupActivity>
    {
        Task<IEnumerable<GroupActivity>> GetByGroupId(int groupId);
        Task<GroupActivity?> GetByGroupAndActivityScheduleId(int groupId, int activityScheduleId);
        Task<GroupActivity?> GetByIdWithGroupAndCampAsync(int groupActivityId);
    }
}
