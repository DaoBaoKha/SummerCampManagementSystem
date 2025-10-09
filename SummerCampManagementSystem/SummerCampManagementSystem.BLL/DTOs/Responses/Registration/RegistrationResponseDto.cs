namespace SummerCampManagementSystem.BLL.DTOs.Responses.Registration
{
    public class CamperSummaryDto
    {
        public int CamperId { get; set; }
        public string CamperName { get; set; }
    }

    public class RegistrationResponseDto
    {
        public int registrationId { get; set; }
        public string CampName { get; set; }
        public int? PaymentId { get; set; }
        public DateTime RegistrationCreateAt { get; set; }
        public string Status { get; set; }

        public List<CamperSummaryDto> Campers { get; set; } = new List<CamperSummaryDto>();
    }
}
