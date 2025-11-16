using SummerCampManagementSystem.BLL.DTOs.User;
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
}
