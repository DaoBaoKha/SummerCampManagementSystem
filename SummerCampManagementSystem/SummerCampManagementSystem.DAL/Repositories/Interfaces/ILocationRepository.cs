using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface ILocationRepository : IGenericRepository<Location>
    {
        Task<IEnumerable<Location>> GetAllCampLocationsAsync();
        Task<bool> IsCampLocationOccupied(int locationId, DateTime startDate, DateTime endDate);

        // admin dashboard method
        Task<List<(int LocationId, string Name, int CampCount, int ActiveCamps)>> GetTopLocationsByCampCountAsync(int limit);
    }
}
