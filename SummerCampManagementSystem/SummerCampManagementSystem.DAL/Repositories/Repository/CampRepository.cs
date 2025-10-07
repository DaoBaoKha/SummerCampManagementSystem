using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class CampRepository : GenericRepository<Camp>, ICampRepository
    {
        public CampRepository(CampEaseDatabaseContext context) : base(context)
        {
        }
        public async Task<IEnumerable<Camp>> GetCampsByTypeAsync(int campTypeId)
        {
            return _context.Camps.Where(c => c.campTypeId == campTypeId).ToList();
        }
    }
}
