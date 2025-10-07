using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class CampTypeRepository : GenericRepository<CampType>, ICampTypeRepository
    {
        public CampTypeRepository(CampEaseDatabaseContext context) : base(context)
        {
        }
        public async Task<CampType?> GetCampTypeByIdAsync(int id)
        {
            return await _context.CampTypes.FindAsync(id);
        }
    }
}
