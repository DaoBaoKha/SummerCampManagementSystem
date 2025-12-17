using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface ICamperTransportRepository : IGenericRepository<CamperTransport>
    {
        Task<IEnumerable<CamperTransport>> GetCamperTransportsByScheduleIdAsync(int scheduleId);
        Task<CamperTransport?> GetCamperTransportByScheduleAndCamperAsync(int transportScheduleId, int camperId);
    }
}
