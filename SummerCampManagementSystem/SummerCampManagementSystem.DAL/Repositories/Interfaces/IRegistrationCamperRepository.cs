using SummerCampManagementSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IRegistrationCamperRepository : IGenericRepository<RegistrationCamper>
    {
        Task<RegistrationCamper?> GetByCamperId(int camperId);
    }
}
