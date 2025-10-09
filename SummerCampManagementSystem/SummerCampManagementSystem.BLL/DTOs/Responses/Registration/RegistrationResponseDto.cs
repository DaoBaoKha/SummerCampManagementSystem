namespace SummerCampManagementSystem.BLL.DTOs.Responses.Registration
{
    public class RegistrationResponseDto
    {
        public int registrationId { get; set; }
        public int CamperId { get; set; }
        public int CampId { get; set; }
        public int PaymentId { get; set; }
        public int? appliedPromotionId { get; set; }
        public DateTime RegistrationCreateAt { get; set; }
        public string Status { get; set; }
    }
}
