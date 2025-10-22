using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.Vehicle;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public VehicleService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<VehicleResponseDto> CreateVehicleAsync(VehicleRequestDto dto)
        {
            var vehicle = _mapper.Map<Vehicle>(dto);
            await _unitOfWork.Vehicles.CreateAsync(vehicle);
            await _unitOfWork.CommitAsync();
            return _mapper.Map<VehicleResponseDto>(vehicle);
        }

        public async Task<bool> DeleteVehicleAsync(int id)
        {
            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(id);
            if (vehicle == null) return false;

            await _unitOfWork.Vehicles.RemoveAsync(vehicle);
            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task<IEnumerable<VehicleResponseDto>> GetAllVehicles()
        {
            var vehicles =  await _unitOfWork.Vehicles.GetAllAsync();
            return _mapper.Map<IEnumerable<VehicleResponseDto>>(vehicles);
        }
        public async Task<VehicleResponseDto?> GetVehicleById(int id)
        {
            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(id);
            return vehicle == null ? null : _mapper.Map<VehicleResponseDto>(vehicle);
        }

        public async Task<bool> UpdateVehicleAsync(int id, VehicleRequestDto vehicle)
        {
            var existingVehicle = await _unitOfWork.Vehicles.GetByIdAsync(id);
            if (existingVehicle == null) return false;
            _mapper.Map(vehicle, existingVehicle);
            await _unitOfWork.Vehicles.UpdateAsync(existingVehicle);
            await _unitOfWork.CommitAsync();
            return true;
        }
    }
}
