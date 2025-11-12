using SummerCampManagementSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface ICamperActivityRepository : IGenericRepository<CamperActivity>
    {
        new Task<IEnumerable<CamperActivity>> GetAllAsync();
        new Task<CamperActivity?> GetByIdAsync(int id);
        Task<bool> IsApprovedAsync(int camperId, int activityId);
        Task<int> CamperofOptionalActivityCount(int activityScheduleId);
        Task<IEnumerable<int?>> GetCamperIdsInOptionalAsync(int optionalActivityId);
    }
}
