using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly IAdminDashboardService _adminDashboardService;

        public DashboardController(IDashboardService dashboardService, IAdminDashboardService adminDashboardService)
        {
            _dashboardService = dashboardService;
            _adminDashboardService = adminDashboardService;
        }

        // manager dashboard endpoints
        /// <summary>
        /// Get total summary statistics for a specific camp
        /// </summary>
        [HttpGet("manager/{campId}/summary")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetDashboardSummary(int campId)
        {
            var summary = await _dashboardService.GetDashboardSummaryAsync(campId);
            return Ok(summary);
        }

        /// <summary>
        /// Get Dashboard Analytics for a specific camp
        /// </summary>
        [HttpGet("manager/{campId}/analytics")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetDashboardAnalytics(int campId)
        {
            var analytics = await _dashboardService.GetDashboardAnalyticsAsync(campId);
            return Ok(analytics);
        }

        /// <summary>
        /// Get capacity alerts and recent registrations for a specific camp
        /// </summary>

        [HttpGet("manager/{campId}/operations")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetDashboardOperations(int campId)
        {
            var operations = await _dashboardService.GetDashboardOperationsAsync(campId);
            return Ok(operations);
        }

        /// <summary>
        /// Get system-wide KPI summary for admin
        /// </summary>
        [HttpGet("admin/summary")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminDashboardSummary()
        {
            var summary = await _adminDashboardService.GetDashboardSummaryAsync();
            return Ok(summary);
        }

        /// <summary>
        /// Get user analytics for admin
        /// </summary>
        [HttpGet("admin/user-analytics")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminUserAnalytics()
        {
            var analytics = await _adminDashboardService.GetUserAnalyticsAsync();
            return Ok(analytics);
        }

        /// <summary>
        /// Get location analytics for admin
        /// </summary>
        [HttpGet("admin/location-analytics")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminLocationAnalytics()
        {
            var analytics = await _adminDashboardService.GetLocationAnalyticsAsync();
            return Ok(analytics);
        }

        /// <summary>
        /// Get camp analytics for admin
        /// </summary>
        [HttpGet("admin/camp-analytics")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminCampAnalytics()
        {
            var analytics = await _adminDashboardService.GetCampAnalyticsAsync();
            return Ok(analytics);
        }

        /// <summary>
        /// Get priority actions for admin
        /// </summary>
        [HttpGet("admin/priority-actions")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminPriorityActions()
        {
            var actions = await _adminDashboardService.GetPriorityActionsAsync();
            return Ok(actions);
        }
    }
}
