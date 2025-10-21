using SummerCampManagementSystem.BLL.DTOs.VehicleType;
using SummerCampManagementSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IVehicleTypeService
    {
        Task<List<VehicleTypeResponseDto>> GetAllVehicleTypesAsync();
        Task<List<VehicleTypeResponseDto>> GetActiveVehicleAsync();
        Task<VehicleTypeResponseDto?> GetVehicleTypeByIdAsync(int id);
        Task<VehicleTypeResponseDto> CreateVehicleTypeAsync(VehicleTypeRequestDto type);
        Task<bool> UpdateVehicleTypeAsync(int id, VehicleTypeUpdateDto type);
        Task<bool> DeleteVehicleTypeAsync(int id);

    }
}
