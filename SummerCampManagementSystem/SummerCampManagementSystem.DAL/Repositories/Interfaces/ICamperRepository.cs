using SummerCampManagementSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface ICamperRepository : IGenericRepository<Camper>
    {
        new Task<IEnumerable<Camper>> GetAllAsync();
        new Task<Camper?> GetByIdAsync(int id);
        Task<Registration?> GetRegistrationByCamperIdAsync(int camperId);
        Task<IEnumerable<Camper>> GetCampersByCampId(int campId);
    }
}
