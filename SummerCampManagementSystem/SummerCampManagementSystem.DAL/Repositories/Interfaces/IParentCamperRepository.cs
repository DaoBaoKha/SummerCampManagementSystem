using SummerCampManagementSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IParentCamperRepository : IGenericRepository<ParentCamper>
    {
        Task<IEnumerable<Camper>> GetByParentIdAsync(int parentId);

        Task<IEnumerable<string>> GetParentEmailsByCamperIdAsync(int camperId);
    }
}
