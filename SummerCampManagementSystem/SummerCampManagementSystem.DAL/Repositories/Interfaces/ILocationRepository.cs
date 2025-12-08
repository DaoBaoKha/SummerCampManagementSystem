using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface ILocationRepository : IGenericRepository<Location>
    {
        Task<IEnumerable<Location>> GetAllCampLocationsAsync();
        Task<bool> IsCampLocationOccupied(int locationId, DateTime startDate, DateTime endDate);
    }
}
