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
        public async Task<IEnumerable<Accommodation>> GetByCampId(int campId)
        {
            return await _context.Accommodations
                .Where(a => a.campId == campId)
                .ToListAsync();
        }
        public async Task<Accommodation?> GetBySupervisorIdAsync(int supervisorId, int campId)
        {
            return await _context.Accommodations
                .Include(a => a.camp)
                .FirstOrDefaultAsync(a => a.supervisorId == supervisorId && a.campId == campId);
        }

        public async Task<bool> isSupervisorOfAccomodation(int supervisorId, int campId)
        {
            return await _context.Accommodations
                .AnyAsync(a => a.supervisorId == supervisorId && a.campId == campId);
                
        }
    }
}
