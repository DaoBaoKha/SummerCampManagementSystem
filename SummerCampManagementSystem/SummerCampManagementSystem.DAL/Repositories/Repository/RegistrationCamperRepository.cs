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
    public class RegistrationCamperRepository : GenericRepository<RegistrationCamper>, IRegistrationCamperRepository
    {
        public RegistrationCamperRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }
        public async Task<RegistrationCamper?> GetByCamperId(int camperId)
        {
            return await _context.RegistrationCampers.FirstOrDefaultAsync
                (rc => rc.camperId == camperId);
        }

        public async Task<RegistrationCamper?> GetByCamperIdAsync(int camperId)
        {
            return await _context.RegistrationCampers
                .Include(rc => rc.registration)
                .Include(rc => rc.camper)
                .FirstOrDefaultAsync(rc => rc.camperId == camperId);
        }

        public async Task<RegistrationCamper?> GetByCamperIdAndCampIdAsync(int camperId, int campId)
        {
            return await _context.RegistrationCampers
                .Include(rc => rc.registration)
                .Include(rc => rc.camper)
                .FirstOrDefaultAsync(rc => rc.camperId == camperId && rc.registration.campId == campId);
        }

        public async Task<IEnumerable<RegistrationCamper>> GetByCampIdAsync(int campId)
        {
            return await _context.RegistrationCampers
                .Include(rc => rc.registration)
                .Where(rc => rc.registration.campId == campId)
                .ToListAsync();
        }
    }
}
