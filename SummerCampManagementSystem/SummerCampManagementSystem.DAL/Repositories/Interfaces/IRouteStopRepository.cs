using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IRouteStopRepository : IGenericRepository<RouteStop>
    {
        IQueryable<RouteStop> GetRouteStopsWithIncludes();
    }
}
