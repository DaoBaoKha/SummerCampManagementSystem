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
                .Include(c => c.HealthRecord) 
                .ToListAsync();
        }

        public new async Task<Camper?> GetByIdAsync(int id)
        {
            return await _context.Campers
                .Include(c => c.HealthRecord) 
                .FirstOrDefaultAsync(c => c.camperId == id);
        }

        public async Task<Registration?> GetRegistrationByCamperIdAsync(int camperId)
        {
            var camper = await _context.Campers
                .Include(c => c.registrations)
                .FirstOrDefaultAsync(c => c.camperId == camperId);

            return camper?.registrations.FirstOrDefault();
        }

        public async Task<IEnumerable<Camper>> GetCampersByCampId(int campId)
        {
            return await _context.Campers
                .Where(c => c.registrations.Any(r => r.campId == campId))
                .ToListAsync();
        }
    }
}
