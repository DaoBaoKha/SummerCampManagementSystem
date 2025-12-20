using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

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

        public async Task<IEnumerable<string>> GetParentEmailsByCamperIdAsync(int camperId)
        {
            return await _context.ParentCampers
                .Where(pc => pc.camperId == camperId)
                .Select(pc => pc.parent.email)
                .Where(email => !string.IsNullOrEmpty(email))
                .Distinct()
                .ToListAsync();
        }
    }
}
