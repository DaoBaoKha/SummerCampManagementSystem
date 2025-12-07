using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class AccommodationActivityRepository : GenericRepository<AccommodationActivitySchedule>, IAccommodationActivityRepository
    {
        public AccommodationActivityRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AccommodationActivitySchedule>> GetByAccommodationIdAsync(int accommodationId)
        {
            return await _context.AccommodationActivitySchedules
                .Where(a => a.accommodationId == accommodationId)
                .ToListAsync();
        }
    }
}
