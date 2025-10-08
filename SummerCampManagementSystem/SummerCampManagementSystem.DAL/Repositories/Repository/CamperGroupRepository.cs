using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class CamperGroupRepository : GenericRepository<CamperGroup>, ICamperGroupRepository
    {
        public CamperGroupRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }
    }
}
