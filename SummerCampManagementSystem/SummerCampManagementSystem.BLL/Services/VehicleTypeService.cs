using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.Vehicle;
using SummerCampManagementSystem.BLL.DTOs.VehicleType;
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
        private readonly IMapper _mapper;
        public VehicleTypeService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<VehicleTypeResponseDto> CreateVehicleTypeAsync(VehicleTypeRequestDto type)
        {
            var vehicleType = _mapper.Map<VehicleType>(type);
            await _unitOfWork.VehicleTypes.CreateAsync(vehicleType);
            await _unitOfWork.CommitAsync();
            return _mapper.Map<VehicleTypeResponseDto>(vehicleType);
        }

        public async Task<bool> DeleteVehicleTypeAsync(int id)
        {
            var type = await _unitOfWork.VehicleTypes.GetByIdAsync(id);
            if (type == null) return false;

            await _unitOfWork.VehicleTypes.RemoveAsync(type);
            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task<List<VehicleTypeResponseDto>> GetActiveVehicleAsync()
        {
            var activeTypes = await _unitOfWork.VehicleTypes.GetActiveTypesAsync();
            return _mapper.Map<List<VehicleTypeResponseDto>>(activeTypes);
        }

        public async Task<List<VehicleTypeResponseDto>> GetAllVehicleTypesAsync()
        {
            var vehicleTypes = await _unitOfWork.VehicleTypes.GetAllAsync();
            return _mapper.Map<List<VehicleTypeResponseDto>>(vehicleTypes);
        }

        public async Task<VehicleTypeResponseDto?> GetVehicleTypeByIdAsync(int id)
        {
            var vehicleType = await _unitOfWork.VehicleTypes.GetByIdAsync(id);
            return vehicleType == null ? null : _mapper.Map<VehicleTypeResponseDto>(vehicleType);
        }

        public async Task<bool> UpdateVehicleTypeAsync(int id, VehicleTypeUpdateDto type)
        {
            var existingType = await _unitOfWork.VehicleTypes.GetByIdAsync(id);
            if (existingType == null) return false;
            _mapper.Map(type, existingType);
            await _unitOfWork.VehicleTypes.UpdateAsync(existingType);
            await _unitOfWork.CommitAsync();
            return true;
        }
    }
}
