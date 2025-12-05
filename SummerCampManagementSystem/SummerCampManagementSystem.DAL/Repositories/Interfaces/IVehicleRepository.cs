using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.DAL.Repositories.Interfaces
{
    public interface IVehicleRepository : IGenericRepository<Vehicle>
    {
        Task<IEnumerable<Vehicle>> GetAllVehiclesWithType();
        Task<Vehicle?> GetVehicleWithTypeById(int id);
        Task<IEnumerable<Vehicle>> GetAvailableVehicles();
    }
}
