using SummerCampManagementSystem.BLL.DTOs.User;
using SummerCampManagementSystem.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.DTOs.Driver
{
    public class DriverResponseDto
    {
        public int DriverId { get; set; }
        public int UserId { get; set; }
        public string LicenseNumber { get; set; } = string.Empty;
        public DateOnly LicenseExpiry { get; set; }
        public string DriverAddress { get; set; } = string.Empty;

        // userAccount navigation property
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }
    }

    public class DriverDetailsDto
    {
        public string LicenseNumber { get; set; } = string.Empty;
        public DateOnly LicenseExpiry { get; set; }
        public string DriverAddress { get; set; } = string.Empty;
    }

    public class DriverNameDto
    {
        public int DriverId { get; set; }
        public string FullName { get; set; } = string.Empty;
    }

    public class DriverRegisterResponseDto : RegisterUserResponseDto
    {
        public DriverDetailsDto? DriverDetails { get; set; }

        public string? OneTimeUploadToken { get; set; }
    }
}
