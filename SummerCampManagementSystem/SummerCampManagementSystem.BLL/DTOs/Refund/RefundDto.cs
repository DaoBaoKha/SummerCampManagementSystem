using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using SummerCampManagementSystem.Core.Enums;

namespace SummerCampManagementSystem.BLL.DTOs.Refund
{
    public class RefundDto
    {
    }

    public class RefundCalculationDto
    {
        public decimal TotalAmountPaid { get; set; }
        public decimal RefundAmount { get; set; }
        public int RefundPercentage { get; set; } 
        public string PolicyDescription { get; set; } = string.Empty;
    }

    public class CancelRequestDto
    {
        public int RegistrationId { get; set; }
        public int BankUserId { get; set; } 
        public string Reason { get; set; } = string.Empty;
    }

    public class RegistrationCancelResponseDto
    {
        public int RegistrationCancelId { get; set; }
        public int RegistrationId { get; set; }
        public decimal RefundAmount { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class RefundRequestListDto
    {
        public int RegistrationCancelId { get; set; }
        public int RegistrationId { get; set; }
        public string ParentName { get; set; } = string.Empty;
        public string ParentEmail { get; set; } = string.Empty;
        public string ParentPhone { get; set; } = string.Empty;
        public List<string> CamperNames { get; set; } = new List<string>();
        public decimal RefundAmount { get; set; }
        public DateTime RequestDate { get; set; } 
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string BankNumber { get; set; } = string.Empty;
        public string BankAccountName { get; set; } = string.Empty;
        public DateTime? ApprovalDate { get; set; }
        public string? ManagerNote { get; set; }
        public string? ImageRefund { get; set; }
        public string? TransactionCode { get; set; }
    }

    public class ApproveRefundDto
    {
        [Required]
        public int RegistrationCancelId { get; set; }

        [Required(ErrorMessage = "Bắt buộc phải có ảnh bằng chứng chuyển khoản.")]
        public IFormFile RefundImage { get; set; } = null!;

        [Required(ErrorMessage = "Mã giao dịch ngân hàng là bắt buộc.")]
        public string TransactionCode { get; set; } = string.Empty;

        public string? ManagerNote { get; set; }
    }

    public class RejectRefundDto
    {
        [Required]
        public int RegistrationCancelId { get; set; }

        [Required(ErrorMessage = "Lý do từ chối là bắt buộc.")]
        public string RejectReason { get; set; } = string.Empty;
    }

    public class RefundRequestFilterDto
    {
        public RegistrationCancelStatus? Status { get; set; } 
    }
}
