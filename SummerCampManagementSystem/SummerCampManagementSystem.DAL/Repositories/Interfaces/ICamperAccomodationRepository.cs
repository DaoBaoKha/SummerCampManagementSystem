using SummerCampManagementSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface ICamperAccomodationRepository : IGenericRepository<CamperAccommodation>
    {
        Task<bool> IsAccommodationStaffOfCamper(int staffId, int camperId);
    }
}
