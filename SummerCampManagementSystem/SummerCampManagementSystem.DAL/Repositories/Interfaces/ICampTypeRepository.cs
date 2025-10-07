using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface ICampTypeRepository : IGenericRepository<CampType>
    {
        Task<CampType?> GetCampTypeByIdAsync(int id);
    }
}
