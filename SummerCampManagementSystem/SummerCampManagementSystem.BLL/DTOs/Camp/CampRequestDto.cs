using SummerCampManagementSystem.Core.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.Camp
{
    public class CampRequestDto
    {
        [Required]
        [StringLength(255)]
        public string? Name { get; set; }

        [Required]
        public string? Description { get; set; }

        [Required]
        public string? Place { get; set; }

        [Required]
        public string? Address { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int? MinParticipants { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int? MaxParticipants { get; set; }

        [Required]
        [Range(0, 100)]
        public int? MinAge { get; set; }

        [Required]
        [Range(0, 100)]
        public int? MaxAge { get; set; }

        [Required]
        public DateTime? StartDate { get; set; }

        [Required]
        public DateTime? EndDate { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Price { get; set; }

        [Required]
        public int? CampTypeId { get; set; }

        public string? Image { get; set; }

        [Required]
        public int? LocationId { get; set; }

        public int? PromotionId { get; set; }

        [Required]
        public DateTime? RegistrationStartDate { get; set; }

        [Required]
        public DateTime? RegistrationEndDate { get; set; }

    }

    public class CampStatusUpdateRequestDto
    {
        public CampStatus Status { get; set; }
    }

    public class CampRejectRequestDto
    {
        [Required(ErrorMessage = "Lý do từ chối là bắt buộc.")]
        [MinLength(10, ErrorMessage = "Lý do từ chối phải có ít nhất 10 ký tự.")]
        public string Note { get; set; } = string.Empty;
    }

    public class CampCancelRequestDto
    {
        [Required(ErrorMessage = "Lý do hủy trại là bắt buộc.")]
        [MinLength(10, ErrorMessage = "Lý do hủy trại phải có ít nhất 10 ký tự.")]
        public string Note { get; set; } = string.Empty;
    }

    public class CampExtensionDto
    {
        [Required(ErrorMessage = "Ngày đóng đăng ký mới là bắt buộc.")]
        public DateTime NewRegistrationEndDate { get; set; }
    }
}