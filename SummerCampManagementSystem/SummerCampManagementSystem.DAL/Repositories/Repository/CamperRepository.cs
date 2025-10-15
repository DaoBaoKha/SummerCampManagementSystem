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
    public class CamperRepository : GenericRepository<Camper>, ICamperRepository
    {
        public CamperRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public new async Task<IEnumerable<Camper>> GetAllAsync()
        {
            return await _context.Campers
                .Include(c => c.HealthRecords)
                .ToListAsync();
        }

        public new async Task<Camper?> GetByIdAsync(int id)
        {
            return await _context.Campers
                .Include(c => c.HealthRecords)
                .FirstOrDefaultAsync(c => c.camperId == id);
        }
    }
}
