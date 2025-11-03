using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.User
{
    public class EmailDto
    {
        public class EmailUpdateRequestDto
        {
            [Required(ErrorMessage = "Email mới là bắt buộc.")]
            [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
            public string NewEmail { get; set; }

            [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc.")]
            public string CurrentPassword { get; set; }
        }

        public class EmailUpdateVerificationDto
        {
            [Required(ErrorMessage = "Email mới là bắt buộc.")]
            [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
            public string NewEmail { get; set; }

            [Required(ErrorMessage = "Mã OTP là bắt buộc.")]
            [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP phải có 6 chữ số.")]
            public string Otp { get; set; }
        }
    }
}
