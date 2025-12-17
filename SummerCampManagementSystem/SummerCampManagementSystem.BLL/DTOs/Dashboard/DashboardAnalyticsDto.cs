namespace SummerCampManagementSystem.BLL.DTOs.Dashboard
{
    public class DashboardAnalyticsDto
    {
        public List<RegistrationTrendDto> RegistrationTrend { get; set; }
        public Dictionary<string, int> StatusDistribution { get; set; }
        public CamperProfileDto CamperProfile { get; set; }
    }

    public class RegistrationTrendDto
    {
        public string Date { get; set; }
        public int Count { get; set; }
        public decimal Revenue { get; set; }
    }

    public class CamperProfileDto
    {
        public Dictionary<string, int> Gender { get; set; }
        public Dictionary<string, int> AgeGroups { get; set; }
    }
}
