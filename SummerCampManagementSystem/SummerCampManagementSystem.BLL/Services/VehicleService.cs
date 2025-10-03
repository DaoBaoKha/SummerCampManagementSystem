using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.VehicleRepository;

namespace SummerCampManagementSystem.BLL.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly IVehicleRepository _vehicleRepository;
        public VehicleService(IVehicleRepository vehicleRepository)
        {
            _vehicleRepository = vehicleRepository;
        }
        public async Task<IEnumerable<Vehicle>> GetAllVehicles()
        {
            return await _vehicleRepository.GetAllVehicles();
        }
        public async Task<Vehicle?> GetVehicleById(int id)
        {
            return await _vehicleRepository.GetVehicleById(id);
        }
    }
}
