using SummerCampManagementSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IGuardianRepository : IGenericRepository<Guardian>
    {
        new Task<IEnumerable<Guardian>> GetAllAsync();
        new Task<Guardian?> GetByIdAsync(int id);
    }
}
