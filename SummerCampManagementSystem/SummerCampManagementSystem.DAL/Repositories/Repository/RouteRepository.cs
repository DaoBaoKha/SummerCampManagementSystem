using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class RouteRepository : GenericRepository<Route>, IRouteRepository
    {
        public RouteRepository(CampEaseDatabaseContext context) : base(context)
        {
        }
    }
}
