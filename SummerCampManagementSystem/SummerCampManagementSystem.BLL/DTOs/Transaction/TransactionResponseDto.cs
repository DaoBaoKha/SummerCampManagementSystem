namespace SummerCampManagementSystem.BLL.DTOs.Transaction
{
    public class TransactionResponseDto
    {
        public int TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; 
        public DateTime TransactionTime { get; set; }
        public string Method { get; set; } = string.Empty;
        public string TransactionCode { get; set; } = string.Empty; 
        public int RegistrationId { get; set; }
        public string CampName { get; set; } = string.Empty;
        public int UserId { get; set; }
    }
}
