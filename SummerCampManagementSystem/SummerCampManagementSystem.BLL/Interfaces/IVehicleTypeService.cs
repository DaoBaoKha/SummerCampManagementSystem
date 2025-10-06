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
        Task<List<VehicleType>> GetAllVehicleTypesAsync();
        Task<List<VehicleType>> GetActiveVehicleAsync();
        Task<VehicleType?> GetVehicleTypeByIdAsync(int id);
        Task CreateVehicleTypeAsync(VehicleType type);
        Task UpdateVehicleTypeAsync(VehicleType type);
        Task DeleteVehicleTypeAsync(int id);

    }
}
