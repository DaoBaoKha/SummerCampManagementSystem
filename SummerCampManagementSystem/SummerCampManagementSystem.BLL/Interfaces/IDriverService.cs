using Microsoft.AspNetCore.Http;
using SummerCampManagementSystem.BLL.DTOs.Driver;
using SummerCampManagementSystem.BLL.DTOs.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SummerCampManagementSystem.BLL.DTOs.Driver.DriverResponseDto;

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
    }
}
