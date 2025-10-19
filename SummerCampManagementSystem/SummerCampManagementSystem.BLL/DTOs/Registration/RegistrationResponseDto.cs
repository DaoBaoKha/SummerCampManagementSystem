using SummerCampManagementSystem.BLL.DTOs.Promotion;

namespace SummerCampManagementSystem.BLL.DTOs.Registration
{
    public class CamperSummaryDto
    {
        public int CamperId { get; set; }
        public string CamperName { get; set; } = string.Empty;
    }

    public class RegistrationResponseDto
    {
        public int registrationId { get; set; }
        public string CampName { get; set; }
        public DateTime RegistrationCreateAt { get; set; }
        public string Note { get; set; }
        public string Status { get; set; }
        public PromotionSummaryDto AppliedPromotion { get; set; }

        public List<CamperSummaryDto> Campers { get; set; } = new List<CamperSummaryDto>();
    }

    public class UpdateRegistrationResponseDto
    {
        public int RegistrationId { get; set; }
        public decimal NewAmount { get; set; }
        public string NewPaymentUrl { get; set; }
    }

    public class GeneratePaymentLinkResponseDto
    {
        public int RegistrationId { get; set; }
        public string Status { get; set; } 
        public decimal Amount { get; set; }
        public string PaymentUrl { get; set; }
    }
}
