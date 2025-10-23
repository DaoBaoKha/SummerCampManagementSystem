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
    }
}
