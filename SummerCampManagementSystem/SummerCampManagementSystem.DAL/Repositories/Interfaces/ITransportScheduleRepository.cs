using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface ITransportScheduleRepository : IGenericRepository<TransportSchedule>   
    {
        IQueryable<TransportSchedule> GetSchedulesWithIncludes();
        Task<IEnumerable<TransportSchedule>> GetSchedulesByCamperIdAsync(int camperId);
        Task<IEnumerable<TransportSchedule>> GetSchedulesByCamperAndCampIdAsync(int camperId, int campId);
    }
}
