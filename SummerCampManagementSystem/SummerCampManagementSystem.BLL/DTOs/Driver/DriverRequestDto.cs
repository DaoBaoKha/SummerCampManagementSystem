using Microsoft.AspNetCore.Http;
using SummerCampManagementSystem.BLL.DTOs.User;
using SummerCampManagementSystem.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.Driver
{

    public class DriverRequestDto
    {
        public string LicenseNumber { get; set; } = null!;
        public DateOnly LicenseExpiry { get; set; }
        public string DriverAddress { get; set; } = null!;
    }

    public class DriverRegisterDto : RegisterUserRequestDto
    {

        [Required(ErrorMessage = "Số giấy phép lái xe là bắt buộc.")]
        [StringLength(255, ErrorMessage = "Số giấy phép không được vượt quá 255 ký tự.")]
        public string LicenseNumber { get; set; } = null!;

        [Required(ErrorMessage = "Ngày hết hạn giấy phép là bắt buộc.")]
        [DataType(DataType.Date)]
        public DateOnly LicenseExpiry { get; set; }

        [Required(ErrorMessage = "Địa chỉ tài xế là bắt buộc.")]
        [StringLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự.")]
        public string DriverAddress { get; set; } = null!;
    }

    public class DriverLicensePhotoUploadDto
    {
        [Required(ErrorMessage = "Ảnh giấy phép lái xe là bắt buộc.")]
        public IFormFile LicensePhoto { get; set; } = null!;
    }

    public class DriverStatusUpdateDto
    {
        [Required(ErrorMessage = "Trạng thái là bắt buộc.")]
        public DriverStatus Status { get; set; }
    }

    public class DriverLicenseUploadByTokenDto
    {
        [Required(ErrorMessage = "Token upload là bắt buộc.")]
        [StringLength(50, ErrorMessage = "Token không hợp lệ.")]
        public string UploadToken { get; set; } = null!;

        [Required(ErrorMessage = "Ảnh giấy phép lái xe là bắt buộc.")]
        public IFormFile LicensePhoto { get; set; } = null!;
    }
}
