using SummerCampManagementSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IAccommodationRepository : IGenericRepository<Accommodation>
    {
        Task<IEnumerable<Accommodation>> GetByCampId(int campId);
        Task<Accommodation?> GetBySupervisorIdAsync(int supervisorId, int campId);
        Task<bool> isSupervisorOfAccomodation(int supervisorId, int campId);
        Task<List<(string Name, int Current, int Max)>> GetCapacityAlertsByCampAsync(int campId);
        Task<List<int?>> GetAccommodationIdsWithSchedulesAsync(int campId);
    }
}
