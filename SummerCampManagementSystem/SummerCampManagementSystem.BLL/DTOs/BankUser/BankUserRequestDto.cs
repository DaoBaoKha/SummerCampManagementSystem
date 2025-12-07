using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.BankUser
{
    public class BankUserRequestDto
    {
        [Required(ErrorMessage = "Mã ngân hàng (Bank Code) là bắt buộc.")]
        [StringLength(50, ErrorMessage = "Mã ngân hàng không được vượt quá 50 ký tự.")]
        public string BankCode { get; set; } = string.Empty; // VCB, SCB, ACB

        [Required(ErrorMessage = "Tên ngân hàng là bắt buộc.")]
        [StringLength(255, ErrorMessage = "Tên ngân hàng không được vượt quá 255 ký tự.")]
        public string BankName { get; set; } = string.Empty; // Vietcombank, Sacombank, ACB

        [Required(ErrorMessage = "Số tài khoản là bắt buộc.")]
        [StringLength(50, ErrorMessage = "Số tài khoản không được vượt quá 50 ký tự.")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Số tài khoản chỉ được chứa ký tự số.")]
        public string BankNumber { get; set; } = string.Empty;

        public bool IsPrimary { get; set; } = false;
    }
}
