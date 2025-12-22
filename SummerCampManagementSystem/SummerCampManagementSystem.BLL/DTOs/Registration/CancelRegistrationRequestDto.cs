using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.Registration
{
    public class CancelRegistrationRequestDto
    {
        public int? BankUserId { get; set; }

        [StringLength(500, ErrorMessage = "Lý do hủy không được vượt quá 500 ký tự.")]
        public string? Reason { get; set; }
    }
}
