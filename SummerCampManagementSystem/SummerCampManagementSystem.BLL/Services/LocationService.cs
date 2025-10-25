using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.Location;
using SummerCampManagementSystem.BLL.DTOs.Shared;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class LocationService : ILocationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public LocationService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<SuccessResponseDto<LocationResponseDto>> CreateLocationAsync(LocationRequestDto location)
        {
            var newLocation = _mapper.Map<Location>(location);
            newLocation.isActive = true; 

            await _unitOfWork.Locations.CreateAsync(newLocation);
            await _unitOfWork.CommitAsync();

            var responseData = _mapper.Map<LocationResponseDto>(newLocation);

            return new SuccessResponseDto<LocationResponseDto>
            {
                Message = "Tạo địa điểm thành công.",
                Data = responseData
            };
        }

        public async Task<MessageResponseDto> DeleteLocationAsync(int id)
        {
            var location = await _unitOfWork.Locations.GetByIdAsync(id);

            if (location == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy địa điểm với ID {id}.");
            }

            await _unitOfWork.Locations.RemoveAsync(location);
            await _unitOfWork.CommitAsync();
            return new MessageResponseDto { Message = "Xóa địa điểm thành công." };
        }

        public async Task<LocationResponseDto> GetLocationByIdAsync(int id)
        {
            var location = await _unitOfWork.Locations.GetByIdAsync(id);
            return location == null ? null : _mapper.Map<LocationResponseDto>(location);
        }

        public async Task<IEnumerable<LocationResponseDto>> GetLocationsAsync()
        {
            return await _unitOfWork.Locations.GetQueryable()
                .ProjectTo<LocationResponseDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<SuccessResponseDto<LocationResponseDto>> UpdateLocationAsync(int id, LocationRequestDto location)
        {
            var existingLocation = await _unitOfWork.Locations.GetByIdAsync(id);
            if (existingLocation == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy địa điểm với ID {id} để cập nhật.");
            }

            _mapper.Map(location, existingLocation);

            await _unitOfWork.Locations.UpdateAsync(existingLocation);
            await _unitOfWork.CommitAsync();

            var responseData = _mapper.Map<LocationResponseDto>(existingLocation);

            return new SuccessResponseDto<LocationResponseDto>
            {
                Message = "Cập nhật địa điểm thành công.",
                Data = responseData
            };
        }
    }
}
