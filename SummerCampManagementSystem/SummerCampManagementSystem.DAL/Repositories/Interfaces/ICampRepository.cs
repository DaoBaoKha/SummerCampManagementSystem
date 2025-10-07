using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface ICampRepository : IGenericRepository<Camp>
    {
        Task<IEnumerable<Camp>> GetCampsByTypeAsync(int campTypeId);
    }
}
