namespace SummerCampManagementSystem.BLL.DTOs.StatisticalReport
{
    public class FinancialTransactionItemDto
    {
        public string TransactionCode { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string PayerName { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string PaymentMethod { get; set; }
    }
}
