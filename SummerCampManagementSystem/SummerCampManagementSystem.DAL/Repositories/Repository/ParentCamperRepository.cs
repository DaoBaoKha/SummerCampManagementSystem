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
    public class ParentCamperRepository : GenericRepository<ParentCamper>, IParentCamperRepository
    {
        public ParentCamperRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Camper>> GetByParentIdAsync(int parentId)
        {
            return await _context.ParentCampers
                .Where(pc => pc.parentId == parentId)
                .Select(pc => pc.camper)
                .ToListAsync();
          
        }
    }
}
