namespace SummerCampManagementSystem.BLL.DTOs.Dashboard
{
    public class ManagerDashboardResponseDto
    {
        public DashboardSummaryDto Summary { get; set; }
        public DashboardAnalyticsDto Analytics { get; set; }
        public DashboardOperationsDto Operations { get; set; }
    }
}
