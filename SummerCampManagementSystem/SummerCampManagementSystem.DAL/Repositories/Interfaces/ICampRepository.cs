using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface ICampRepository : IGenericRepository<Camp>
    {
        Task<IEnumerable<Camp>> GetCampsByTypeAsync(int campTypeId);
        Task<IEnumerable<Camp>> GetCampsByStaffIdAsync(int staffId);
    }
}
