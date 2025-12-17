namespace SummerCampManagementSystem.BLL.DTOs.Dashboard
{
    public class AdminDashboardSummaryDto
    {
        public KpiMetricDto TotalRevenue { get; set; }
        public KpiMetricDto TotalCustomers { get; set; }
        public KpiMetricDto TotalWorkforce { get; set; }
        public int TotalActiveCamps { get; set; }
    }

    public class KpiMetricDto
    {
        public decimal Value { get; set; }
        public double? Growth { get; set; }
        public string Label { get; set; }
    }
}
