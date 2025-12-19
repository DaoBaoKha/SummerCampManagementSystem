using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Report;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/report")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly IUserContextService _userContextService;

        public ReportController(IReportService reportService, IUserContextService userContextService)
        {
            _reportService = reportService;
            _userContextService = userContextService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllReports()
        {
            var reports = await _reportService.GetAllReportsAsync();
            return Ok(reports);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetReportById(int id)
        {
            var report = await _reportService.GetReportByIdAsync(id);
            return Ok(report);
        }

        /// <summary>
        /// Get reports by camper
        /// </summary>
        [HttpGet("camper/{camperId}")]
        public async Task<IActionResult> GetReportsByCamper(int camperId, [FromQuery] int? campId = null)
        {
            var reports = await _reportService.GetReportsByCamperAsync(camperId, campId);
            return Ok(reports);
        }


        /// <summary>
        /// Get reports by type
        /// </summary>
        [HttpGet("type/{reportType}")]
        public async Task<IActionResult> GetReportsByType(ReportType reportType)
        {
            var reports = await _reportService.GetReportsByTypeAsync(reportType);
            return Ok(reports);
        }


        /// <summary>
        /// Get reports by login staff
        /// </summary>
        [Authorize(Roles = "Staff")]
        [HttpGet("my-reports")]
        public async Task<IActionResult> GetMyReports()
        {
            var staffId = _userContextService.GetCurrentUserId();
            if (!staffId.HasValue)
            {
                return Unauthorized(new { message = "Unable to identify current user" });
            }

            var reports = await _reportService.GetReportsByStaffAsync(staffId.Value);
            return Ok(reports);
        }

        /// <summary>
        /// Get all reports by camp
        /// </summary>
        [HttpGet("camp/{campId}")]
        public async Task<IActionResult> GetReportsByCamp(int campId)
        {
            var reports = await _reportService.GetReportsByCampAsync(campId);
            return Ok(reports);
        }

        [Authorize(Roles = "Staff")]
        [HttpPost]
        public async Task<IActionResult> CreateReport([FromBody] ReportRequestDto report)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var staffId = _userContextService.GetCurrentUserId();
                var createdReport = await _reportService.CreateReportAsync(report, staffId.Value);
                return CreatedAtAction(nameof(GetReportById), new { id = createdReport.reportId }, createdReport);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error", detail = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBlogPost(int id, [FromForm] ReportRequestDto report)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var updatedReport = await _reportService.UpdateReportAsync(id, report);
                if (updatedReport == null)
                {
                    return NotFound();
                }
                return Ok(updatedReport);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error", detail = ex.Message });
            }

        }

        // DELETE api/<ReportController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReport(int id)
        {
            var result = await _reportService.DeleteReportAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }

        /// <summary>
        /// Report transport incident when camper drops out mid-journey
        /// </summary>
        [Authorize(Roles = "Staff")]
        [HttpPost("transport-incident")]
        public async Task<IActionResult> CreateTransportIncident([FromBody] TransportIncidentRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var staffId = _userContextService.GetCurrentUserId();
            if (!staffId.HasValue)
            {
                return Unauthorized(new { message = "Unable to identify current user" });
            }

            var report = await _reportService.CreateTransportIncidentAsync(dto, staffId.Value);
            return CreatedAtAction(nameof(GetReportById), new { id = report.reportId }, report);
        }

        /// <summary>
        /// Report early checkout of camper
        /// </summary>
        [Authorize(Roles = "Staff")]
        [HttpPost("early-checkout")]
        public async Task<IActionResult> CreateEarlyCheckout([FromBody] EarlyCheckoutRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var staffId = _userContextService.GetCurrentUserId();
            if (!staffId.HasValue)
            {
                return Unauthorized(new { message = "Unable to identify current user" });
            }

            var report = await _reportService.CreateEarlyCheckoutReportAsync(dto, staffId.Value);
            return CreatedAtAction(nameof(GetReportById), new { id = report.reportId }, report);
        }

        /// <summary>
        /// Create a general incident ticket
        /// </summary>
        [Authorize(Roles = "Staff")]
        [HttpPost("incident-ticket")]
        public async Task<IActionResult> CreateIncidentTicket([FromBody] IncidentTicketRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var staffId = _userContextService.GetCurrentUserId();
            if (!staffId.HasValue)
            {
                return Unauthorized(new { message = "Unable to identify current user" });
            }

            var report = await _reportService.CreateIncidentTicketAsync(dto, staffId.Value);
            return CreatedAtAction(nameof(GetReportById), new { id = report.reportId }, report);
        }
    }
}
