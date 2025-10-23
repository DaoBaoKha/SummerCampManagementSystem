using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IActivityScheduleRepository : IGenericRepository<ActivitySchedule>
    {
        Task<bool> IsTimeOverlapAsync(int? campId, DateTime start, DateTime end);
        Task<ActivitySchedule?> GetByIdWithActivityAsync(int id);
    }
}
