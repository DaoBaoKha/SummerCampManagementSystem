namespace SummerCampManagementSystem.BLL.DTOs.Requests.Registration
{
    public class RegistrationRequestDto
    {
        public List<int> CamperIds { get; set; }
        public int CampId { get; set; }
        public int? PaymentId { get; set; }
        public int? appliedPromotionId { get; set; }

    }
}
