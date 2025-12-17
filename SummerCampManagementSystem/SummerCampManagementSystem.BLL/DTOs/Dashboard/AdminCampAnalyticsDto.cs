namespace SummerCampManagementSystem.BLL.DTOs.Dashboard
{
    public class AdminCampAnalyticsDto
    {
        public Dictionary<string, int> StatusOverview { get; set; }
        public List<MonthlyRevenueDto> MonthlyRevenue { get; set; }
    }

    public class MonthlyRevenueDto
    {
        public string Month { get; set; }
        public decimal Revenue { get; set; }
    }
}
