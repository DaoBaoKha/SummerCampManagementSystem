using SummerCampManagementSystem.BLL.DTOs.Vehicle;
using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IVehicleService
    {
        Task<IEnumerable<VehicleResponseDto>> GetAllVehicles();
        Task<VehicleResponseDto?> GetVehicleById(int id);
        Task<VehicleResponseDto> CreateVehicleAsync(VehicleRequestDto vehicle);
        Task<bool> UpdateVehicleAsync(int id, VehicleRequestDto vehicle);
        Task<bool> DeleteVehicleAsync(int id);
    }
}
