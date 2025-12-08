using SummerCampManagementSystem.BLL.DTOs.Location;
using SummerCampManagementSystem.BLL.DTOs.Shared;
using SummerCampManagementSystem.Core.Enums;
using static SummerCampManagementSystem.BLL.DTOs.Location.LocationRequestDto; 

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface ILocationService
    {
        Task<IEnumerable<LocationResponseDto>> GetLocationsAsync();
        Task<LocationResponseDto> GetLocationByIdAsync(int id);
        Task<SuccessResponseDto<LocationResponseDto>> CreateLocationAsync(LocationCreateDto location);
        Task<SuccessResponseDto<LocationResponseDto>> UpdateLocationAsync(int id, LocationUpdateDto location);
        Task<MessageResponseDto> DeleteLocationAsync(int id);
        Task<IEnumerable<LocationResponseDto>> GetLocationsByTypeAsync(LocationType type);
        Task<IEnumerable<LocationResponseDto>> GetChildLocationsByParentIdAsync(int parentLocationId);
        Task<IEnumerable<LocationResponseDto>> GetChildLocationsByCampIdByTime(int campId, DateTime startTime, DateTime endTime);
        Task<IEnumerable<LocationDto>> GetAvailableCampLocationInDateRange(DateTime startDate, DateTime endDate);
    }
}