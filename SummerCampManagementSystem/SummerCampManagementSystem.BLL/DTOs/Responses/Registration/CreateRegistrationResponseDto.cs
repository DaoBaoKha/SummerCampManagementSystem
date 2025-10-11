namespace SummerCampManagementSystem.BLL.DTOs.Responses.Registration
{
    public class CreateRegistrationResponseDto
    {
        public int RegistrationId { get; set; }
        public string Status { get; set; } // pending
        public decimal Amount { get; set; }
        public string PaymentUrl { get; set; } 
    }
}
