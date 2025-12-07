using SummerCampManagementSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IAccommodationActivityRepository : IGenericRepository<AccommodationActivitySchedule>
    {
        Task<IEnumerable<AccommodationActivitySchedule>> GetByAccommodationIdAsync(int accommodationId);
    }
}
