using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly IUnitOfWork _unitOfWork;
        public VehicleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task CreateVehicleAsync(Vehicle vehicle)
        {
            await _unitOfWork.Vehicles.CreateAsync(vehicle);
            await _unitOfWork.CommitAsync();
        }

        public async Task DeleteVehicleAsync(int id)
        {
            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(id);
            if (vehicle == null) return;

            await _unitOfWork.Vehicles.RemoveAsync(vehicle);
            await _unitOfWork.CommitAsync();
        }

        public async Task<IEnumerable<Vehicle>> GetAllVehicles()
        {
            return await _unitOfWork.Vehicles.GetAllAsync();
        }
        public async Task<Vehicle?> GetVehicleById(int id)
        {
            return await _unitOfWork.Vehicles.GetByIdAsync(id);
        }

        public async Task UpdateVehicleAsync(Vehicle vehicle)
        {
            await _unitOfWork.Vehicles.UpdateAsync(vehicle);
            await _unitOfWork.CommitAsync();
        }
    }
}
