using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Services
{
    public class VehicleTypeService : IVehicleTypeService
    {
        private readonly IUnitOfWork _unitOfWork;
        public VehicleTypeService(IUnitOfWork unitOfWork) 
        {
            _unitOfWork = unitOfWork;
        }
        public async Task CreateVehicleTypeAsync(VehicleType type)
        {
            await _unitOfWork.VehicleTypes.CreateAsync(type);
            await _unitOfWork.CommitAsync();
        }

        public async Task DeleteVehicleTypeAsync(int id)
        {
            var type = await _unitOfWork.VehicleTypes.GetByIdAsync(id);
            if (type == null) return;

            await _unitOfWork.VehicleTypes.RemoveAsync(type);
            await _unitOfWork.CommitAsync();
        }

        public async Task<List<VehicleType>> GetActiveVehicleAsync()
        {
            return await _unitOfWork.VehicleTypes.GetActiveTypesAsync();
        }

        public async Task<List<VehicleType>> GetAllVehicleTypesAsync()
        {
            return await _unitOfWork.VehicleTypes.GetAllAsync();
        }

        public Task<VehicleType?> GetVehicleTypeByIdAsync(int id)
        {
            return _unitOfWork.VehicleTypes.GetByIdAsync(id);
        }

        public async Task UpdateVehicleTypeAsync(VehicleType type)
        {
            await _unitOfWork.VehicleTypes.UpdateAsync(type);
            await _unitOfWork.CommitAsync();
        }
    }
}
