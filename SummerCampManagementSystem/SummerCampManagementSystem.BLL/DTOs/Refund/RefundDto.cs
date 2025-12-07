namespace SummerCampManagementSystem.BLL.DTOs.Refund
{
    public class RefundDto
    {
    }

    public class RefundCalculationDto
    {
        public decimal TotalAmountPaid { get; set; }
        public decimal RefundAmount { get; set; }
        public int RefundPercentage { get; set; } 
        public string PolicyDescription { get; set; } = string.Empty;
    }

    public class CancelRequestDto
    {
        public int RegistrationId { get; set; }
        public int BankUserId { get; set; } 
        public string Reason { get; set; } = string.Empty;
    }

    public class RegistrationCancelResponseDto
    {
        public int RegistrationCancelId { get; set; }
        public int RegistrationId { get; set; }
        public decimal RefundAmount { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
