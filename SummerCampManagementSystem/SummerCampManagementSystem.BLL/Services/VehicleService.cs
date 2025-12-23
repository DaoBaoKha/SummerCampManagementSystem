using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.Vehicle;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
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

            // check if vehicle is being used in any active TransportSchedules (NotYet or InProgress)
            var isUsedInActiveTransportSchedule = await _unitOfWork.TransportSchedules.GetQueryable()
                .Where(ts => ts.vehicleId == id 
                    && (ts.status == TransportScheduleStatus.NotYet.ToString() 
                        || ts.status == TransportScheduleStatus.InProgress.ToString()))
                .AnyAsync();

            if (isUsedInActiveTransportSchedule)
            {
                throw new BusinessRuleException("Không thể xóa phương tiện vì đang được sử dụng trong lịch vận chuyển đang hoạt động.");
            }

            await _unitOfWork.Vehicles.RemoveAsync(vehicle);
            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task<IEnumerable<VehicleResponseDto>> GetAllVehicles()
        {
            var vehicles =  await _unitOfWork.Vehicles.GetAllVehiclesWithType();
            return _mapper.Map<IEnumerable<VehicleResponseDto>>(vehicles);
        }
        public async Task<VehicleResponseDto?> GetVehicleById(int id)
        {
            var vehicle = await _unitOfWork.Vehicles.GetVehicleWithTypeById(id);
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

        public async Task<IEnumerable<VehicleResponseDto>> GetAvailableVehicles()
        {
            var availableVehicles = await _unitOfWork.Vehicles.GetAvailableVehicles();
            return _mapper.Map<IEnumerable<VehicleResponseDto>>(availableVehicles);
        }
    }
}
