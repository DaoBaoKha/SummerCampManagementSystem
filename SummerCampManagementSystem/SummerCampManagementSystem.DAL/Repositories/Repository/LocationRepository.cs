using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.DAL.Repositories.Repository
{
    public class LocationRepository : GenericRepository<Location>, ILocationRepository
    {
        public LocationRepository(CampEaseDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Location>> GetAllCampLocationsAsync()
        {
            return await _context.Locations
                .Where(l => l.locationType == "Camp" && l.isActive == true)
                .ToListAsync();
        }

        public async Task<bool> IsCampLocationOccupied(int locationId, DateTime startDate, DateTime endDate)
        {
            return await _context.Camps
                .AnyAsync(c => c.locationId == locationId
                              && c.startDate <= endDate && c.endDate >= startDate     
                         );
        }
    }
}
