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
    public class GuardianRepository : GenericRepository<Guardian>, IGuardianRepository
    {
        private new readonly CampEaseDatabaseContext _context;

        public GuardianRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public new async Task<IEnumerable<Guardian>> GetAllAsync()
        {
            return await _context.Guardians
                .Include(g => g.CamperGuardians)
                .ThenInclude(cg => cg.camper)
                .ToListAsync();
        }

        public new async Task<Guardian?> GetByIdAsync(int id)
        {
            return await _context.Guardians
                .Include(g => g.CamperGuardians)
                .ThenInclude(cg => cg.camper)
                .FirstOrDefaultAsync(g => g.guardianId == id);
        }
    }
}
