using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class CamperTransportRepository : GenericRepository<CamperTransport>, ICamperTransportRepository
    {
        public CamperTransportRepository(CampEaseDatabaseContext context) : base(context)
        {
        }
    }
}
