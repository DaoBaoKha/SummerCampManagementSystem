namespace SummerCampManagementSystem.BLL.DTOs.Registration
{
    public class CreateRegistrationRequestDto
    {
        public List<int> CamperIds { get; set; }
        public int CampId { get; set; }
        public int? appliedPromotionId { get; set; }
        public int? userId { get; set; }
        public string? Note { get; set; }
    }
}
