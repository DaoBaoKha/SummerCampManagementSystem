using SummerCampManagementSystem.BLL.DTOs.Location;
using SummerCampManagementSystem.BLL.DTOs.Shared;
using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface ILocationService
    {
        Task<IEnumerable<LocationResponseDto>> GetLocationsAsync();
        Task<LocationResponseDto> GetLocationByIdAsync(int id);
        Task<SuccessResponseDto<LocationResponseDto>> CreateLocationAsync(LocationRequestDto location);
        Task<SuccessResponseDto<LocationResponseDto>> UpdateLocationAsync(int id, LocationRequestDto location);
        Task<MessageResponseDto> DeleteLocationAsync(int id);
    }
}
