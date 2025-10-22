namespace SummerCampManagementSystem.BLL.DTOs.Registration
{
    public class CreateRegistrationRequestDto
    {
        public List<int> CamperIds { get; set; }
        public int CampId { get; set; }
        public List<OptionalChoiceDto> OptionalChoices { get; set; } = new List<OptionalChoiceDto>();
        public int? appliedPromotionId { get; set; }
        public string? Note { get; set; }
    }

    public class OptionalChoiceDto
    {
        public int CamperId { get; set; }
        public int ActivityScheduleId { get; set; }
    }
}
