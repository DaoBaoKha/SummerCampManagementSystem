using SummerCampManagementSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IReportRepository : IGenericRepository<Report>
    {
        Task<bool> IsCamperOfActivityAsync(int camperId, int activityId);
        Task<IEnumerable<Report>> GetReportsByCamperAsync(int camperId, int? campId = null);
        Task<IEnumerable<Report>> GetReportsByStaffAsync(int staffId);
        Task<IEnumerable<Report>> GetReportsByCampAsync(int campId);
    }
}
