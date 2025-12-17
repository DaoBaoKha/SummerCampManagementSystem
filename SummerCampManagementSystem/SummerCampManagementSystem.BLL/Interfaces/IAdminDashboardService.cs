using SummerCampManagementSystem.BLL.DTOs.Dashboard;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IAdminDashboardService
    {
        Task<AdminDashboardSummaryDto> GetDashboardSummaryAsync();
        Task<AdminUserAnalyticsDto> GetUserAnalyticsAsync();
        Task<AdminLocationAnalyticsDto> GetLocationAnalyticsAsync();
        Task<AdminCampAnalyticsDto> GetCampAnalyticsAsync();
        Task<AdminPriorityActionsDto> GetPriorityActionsAsync();
    }
}
