using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Blog;
using SummerCampManagementSystem.BLL.DTOs.Report;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.BLL.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
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

        // GET: api/<ReportController>
        [HttpGet]
        public async Task<IActionResult> GetAllReports()
        {
            var reports = await _reportService.GetAllReportsAsync();
            return Ok(reports);
        }


        // GET api/<ReportController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReportById(int id)
        {
            var report = await _reportService.GetReportByIdAsync(id);
            if (report == null) return NotFound();
            return Ok(report);
        }

        // POST api/<ReportController>
        [Authorize(Roles = "Staff")]
        [HttpPost]
        public async Task<IActionResult> CreateReport([FromBody] ReportRequestDto report)
        {
            var staffId = _userContextService.GetCurrentUserId();
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

        // PUT api/<ReportController>/5
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
    }
}
