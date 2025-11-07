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
    public class AccommodationRepository : GenericRepository<Accommodation>, IAccommodationRepository
    {
        public AccommodationRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Accommodation>> GetAllBySupervisorIdAsync(int supervisorId)
        {
            return await _context.Accommodations
                .Include(a => a.camp)
                .Where(a => a.supervisorId == supervisorId)
                .ToListAsync();
        }
    }
}
