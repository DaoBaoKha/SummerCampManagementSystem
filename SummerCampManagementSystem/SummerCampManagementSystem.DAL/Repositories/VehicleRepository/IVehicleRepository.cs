using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.GenericRepository;

namespace SummerCampManagementSystem.DAL.Repositories.VehicleRepository
{
    public interface IVehicleRepository : IGenericRepository<Vehicle>
    {
        Task<IEnumerable<Vehicle>> GetAllVehicles();

        Task<Vehicle?> GetVehicleById(int id);
    }
}
