using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.Registration
{
    public class RegistrationRequestDto
    {
 
    }

    public class CreateRegistrationRequestDto
    {
        public List<int> CamperIds { get; set; }
        public int CampId { get; set; }
        public int? appliedPromotionId { get; set; }
        public string? Note { get; set; }
    }

    public class OptionalChoiceDto
    {
        public int CamperId { get; set; }
        public int ActivityScheduleId { get; set; }
    }

    public class TransportChoiceDto
    {
        public int CamperId { get; set; }
        public int TransportScheduleId { get; set; }
        public int LocationId { get; set; }


    }

    public class UpdateRegistrationRequestDto
    {
        public List<int> CamperIds { get; set; }
        public int CampId { get; set; }
        public int? appliedPromotionId { get; set; }
        public string? Note { get; set; }
    }

    public class GeneratePaymentLinkRequestDto
    {
        public List<OptionalChoiceDto>? OptionalChoices { get; set; }

        public List<TransportChoiceDto>? TransportChoices { get; set; }
    }

    public class RejectRegistrationRequestDto
    {
        [Required(ErrorMessage = "ID đơn đăng ký là bắt buộc.")]
        public int RegistrationId { get; set; }

        [Required(ErrorMessage = "Lý do từ chối là bắt buộc.")]
        [StringLength(500, ErrorMessage = "Lý do không được vượt quá 500 ký tự.")]
        public string RejectReason { get; set; } = string.Empty;

        // if null reject all
        public List<int>? CamperIds { get; set; }
    }
}
