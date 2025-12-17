namespace SummerCampManagementSystem.BLL.DTOs.Dashboard
{
    public class AdminUserAnalyticsDto
    {
        public Dictionary<string, int> WorkforceDistribution { get; set; }
        public List<DailyGrowthDto> NewCustomerGrowth { get; set; }
    }

    public class DailyGrowthDto
    {
        public string Date { get; set; }
        public int Count { get; set; }
    }
}
