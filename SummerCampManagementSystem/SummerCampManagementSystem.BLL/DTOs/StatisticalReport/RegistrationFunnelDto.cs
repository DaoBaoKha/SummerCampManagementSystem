namespace SummerCampManagementSystem.BLL.DTOs.StatisticalReport
{
    public class RegistrationFunnelDto
    {
        public int TotalRequests { get; set; }
        public int PendingApproval { get; set; }
        public int Approved { get; set; }
        public int Confirmed { get; set; }
        public int Canceled { get; set; }
        public int Rejected { get; set; }

        // calculated percentages
        public decimal ApprovalRate { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal CancellationRate { get; set; }
    }
}
