using SummerCampManagementSystem.BLL.DTOs.Dashboard;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardSummaryDto> GetDashboardSummaryAsync(int campId);
        Task<DashboardAnalyticsDto> GetDashboardAnalyticsAsync(int campId);
        Task<DashboardOperationsDto> GetDashboardOperationsAsync(int campId);
    }
}
