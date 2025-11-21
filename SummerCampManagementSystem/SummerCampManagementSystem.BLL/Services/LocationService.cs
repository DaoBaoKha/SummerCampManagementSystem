using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.Location;
using SummerCampManagementSystem.BLL.DTOs.Shared;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;
using static SummerCampManagementSystem.BLL.DTOs.Location.LocationRequestDto;

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


        private async Task ValidateLocationLogicAsync(LocationType type, int? parentLocationId, int? currentLocationId = null)
        {
            if (type == LocationType.Camp || type == LocationType.Pickup_point)
            {
                // Luật 1: Camp và Pickup Point phải là cấp cao nhất (ParentId = null)
                if (parentLocationId.HasValue)
                {
                    throw new ArgumentException($"{type} phải là vị trí cấp cao nhất. Không được gán Parent Location.");
                }
            }
            else if (type == LocationType.In_camp)
            {
                // Luật 2: In_camp bắt buộc phải có ParentId
                if (!parentLocationId.HasValue)
                {
                    throw new ArgumentException("Vị trí In_camp bắt buộc phải thuộc về một Camp (Parent Location).");
                }

                var parentLocation = await _unitOfWork.Locations.GetByIdAsync(parentLocationId.Value);

                // Luật 3: Parent Location phải tồn tại
                if (parentLocation == null)
                {
                    throw new KeyNotFoundException($"Không tìm thấy Parent Location với ID {parentLocationId.Value}.");
                }

                // Luật 4: Parent của In_camp phải là loại 'Camp'
                if (parentLocation.locationType != LocationType.Camp.ToString())
                {
                    throw new ArgumentException($"Parent Location phải là loại 'Camp' để chứa vị trí In_camp.");
                }
            }

            // Luật 5 (Update): Kiểm tra không cho phép tự tham chiếu (chỉ khi cập nhật)
            if (currentLocationId.HasValue && parentLocationId.HasValue && currentLocationId.Value == parentLocationId.Value)
            {
                throw new ArgumentException("Không thể gán vị trí làm Parent của chính nó.");
            }
        }


        public async Task<SuccessResponseDto<LocationResponseDto>> CreateLocationAsync(LocationCreateDto location)
        {
            // 1. Áp dụng Logic Validation
            await ValidateLocationLogicAsync(location.LocationType, location.ParentLocationId);

            var newLocation = _mapper.Map<Location>(location);
            newLocation.isActive = true;

            // Gán lại Parent Location ID và chuyển Enum sang string để lưu vào DB
            newLocation.campLocationId = location.ParentLocationId;
            newLocation.locationType = location.LocationType.ToString();

            await _unitOfWork.Locations.CreateAsync(newLocation);
            await _unitOfWork.CommitAsync();

            var responseData = _mapper.Map<LocationResponseDto>(newLocation);

            return new SuccessResponseDto<LocationResponseDto>
            {
                Message = "Tạo địa điểm thành công.",
                Data = responseData
            };
        }

        public async Task<SuccessResponseDto<LocationResponseDto>> UpdateLocationAsync(int id, LocationUpdateDto location)
        {
            var existingLocation = await _unitOfWork.Locations.GetByIdAsync(id);
            if (existingLocation == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy địa điểm với ID {id} để cập nhật.");
            }

            var newLocationType = location.LocationType ?? Enum.Parse<LocationType>(existingLocation.locationType);
            var newParentId = location.ParentLocationId ?? existingLocation.campLocationId; 

            await ValidateLocationLogicAsync(newLocationType, newParentId, id);

            // check if changing location type and has child locations
            if (location.LocationType.HasValue && location.LocationType.Value.ToString() != existingLocation.locationType)
            {
                var hasChildren = await _unitOfWork.Locations.GetQueryable()
                    .AnyAsync(l => l.campLocationId == id);

                if (hasChildren)
                {
                    throw new InvalidOperationException("Không thể thay đổi loại vị trí vì nó đang chứa các vị trí con.");
                }
            }

            _mapper.Map(location, existingLocation);

            existingLocation.campLocationId = newParentId;
            if (location.LocationType.HasValue)
            {
                existingLocation.locationType = location.LocationType.Value.ToString();
            }
            if (location.IsActive.HasValue)
            {
                existingLocation.isActive = location.IsActive.Value;
            }


            await _unitOfWork.Locations.UpdateAsync(existingLocation);
            await _unitOfWork.CommitAsync();

            var responseData = _mapper.Map<LocationResponseDto>(existingLocation);

            return new SuccessResponseDto<LocationResponseDto>
            {
                Message = "Cập nhật địa điểm thành công.",
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

            // check if this location has child locations
            var hasChildren = await _unitOfWork.Locations.GetQueryable()
                .AnyAsync(l => l.campLocationId == id);

            if (hasChildren)
            {
                throw new InvalidOperationException("Không thể xóa vị trí này vì nó đang chứa các vị trí con (Activity Locations).");
            }

            location.isActive = false; // soft delete
            await _unitOfWork.Locations.UpdateAsync(location);

            await _unitOfWork.CommitAsync();
            return new MessageResponseDto { Message = "Xóa địa điểm thành công (Soft Delete)." };
        }



        public async Task<LocationResponseDto> GetLocationByIdAsync(int id)
        {
            var location = await _unitOfWork.Locations.GetQueryable()
                .Include(l => l.campLocation) 
                .FirstOrDefaultAsync(l => l.locationId == id);

            return location == null ? null : _mapper.Map<LocationResponseDto>(location);
        }

        public async Task<IEnumerable<LocationResponseDto>> GetLocationsAsync()
        {
            var locations = await _unitOfWork.Locations.GetQueryable()
                .Include(l => l.campLocation) 
                .ToListAsync();

            return _mapper.Map<IEnumerable<LocationResponseDto>>(locations);
        }

        public async Task<IEnumerable<LocationResponseDto>> GetLocationsByTypeAsync(LocationType type)
        {
            var locations = await _unitOfWork.Locations.GetQueryable()
                .Where(l => l.locationType == type.ToString()) 
                .Include(l => l.campLocation)
                .ToListAsync(); 

            return _mapper.Map<IEnumerable<LocationResponseDto>>(locations);
        }

        public async Task<IEnumerable<LocationResponseDto>> GetChildLocationsByParentIdAsync(int parentLocationId)
        {
            var locations = await _unitOfWork.Locations.GetQueryable()
                .Where(l => l.campLocationId == parentLocationId && l.locationType == LocationType.In_camp.ToString())
                .Include(l => l.campLocation)
                .ToListAsync();

            return _mapper.Map<IEnumerable<LocationResponseDto>>(locations);
        }

        public async Task<IEnumerable<LocationResponseDto>> GetChildLocationsByCampIdByTime(int campId, DateTime startTime, DateTime endTime)
        {
           var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new KeyNotFoundException($"Không tìm thấy Camp với ID {campId}.");

            var parentLocationId = camp.locationId;

            if (parentLocationId == null)
            {
                throw new InvalidOperationException($"Camp với ID {campId} không có Location hợp lệ.");
            }

            var  childLocations = await GetChildLocationsByParentIdAsync(parentLocationId.Value);

            var suitableLocations = new List<LocationResponseDto>();

             foreach (var location in childLocations)
             {
                if (await _unitOfWork.ActivitySchedules.ExistsInSameTimeAndLocationAsync(location.LocationId, startTime, endTime))
                    continue;

                suitableLocations.Add(location);
             }
            return suitableLocations;
        }

    }
}