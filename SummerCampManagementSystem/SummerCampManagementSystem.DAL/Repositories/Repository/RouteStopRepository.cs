using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class RouteStopRepository : GenericRepository<RouteStop>, IRouteStopRepository
    {

        public RouteStopRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public IQueryable<RouteStop> GetRouteStopsWithIncludes()
        {
            return _context.RouteStops
                .Include(rs => rs.location)
                .Include(rs => rs.route);
        }
    }
}
