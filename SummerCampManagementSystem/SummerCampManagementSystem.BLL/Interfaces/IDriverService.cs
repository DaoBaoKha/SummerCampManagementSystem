using Microsoft.AspNetCore.Http;
using SummerCampManagementSystem.BLL.DTOs.Driver;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IDriverService
    {
        Task<DriverResponseDto> GetDriverByUserIdAsync(int userId);
        Task<IEnumerable<DriverResponseDto>> GetAllDriversAsync();
        Task<IEnumerable<DriverResponseDto>> GetDriverByStatusAsync(string status);
        Task<DriverResponseDto> UpdateDriverAsync(int driverId, DriverRequestDto driverRequestDto);
        Task<bool> DeleteDriverAsync(int driverId);
        Task<DriverRegisterResponseDto> RegisterDriverAsync(DriverRegisterDto model);
        Task<string> UpdateDriverAvatarAsync(int userId, IFormFile file);
        Task<string> UpdateDriverLicensePhotoAsync(IFormFile file);
        Task<DriverResponseDto> UpdateDriverStatusAsync(int driverId, DriverStatusUpdateDto updateDto);
        Task<string> UpdateDriverLicensePhotoByTokenAsync(string uploadToken, IFormFile file);
        Task<IEnumerable<DriverResponseDto>> GetAvailableDriversAsync(DateOnly? date, TimeOnly? startTime, TimeOnly? endTime);
    }
}
