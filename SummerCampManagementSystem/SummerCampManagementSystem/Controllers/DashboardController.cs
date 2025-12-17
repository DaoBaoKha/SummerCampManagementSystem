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

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

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
    }
}
