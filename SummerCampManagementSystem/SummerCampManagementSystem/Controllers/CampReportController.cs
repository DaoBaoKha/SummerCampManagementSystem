using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.Controllers
{
    [Route("api/camp-reports")]
    [ApiController]
    public class CampReportController : ControllerBase
    {
        private readonly ICampReportExportService _campReportExportService;

        public CampReportController(ICampReportExportService campReportExportService)
        {
            _campReportExportService = campReportExportService;
        }

        /// <summary>
        /// Export camp performance report to Excel format
        /// </summary>
        [HttpGet("{campId}/export/excel")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ExportCampReportToExcel(int campId)
        {
            var fileBytes = await _campReportExportService.ExportCampReportToExcelAsync(campId);
            var fileName = $"CampReport_{campId}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        /// <summary>
        /// Export camp performance report to PDF format
        /// </summary>
        [HttpGet("{campId}/export/pdf")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ExportCampReportToPdf(int campId)
        {
            var fileBytes = await _campReportExportService.ExportCampReportToPdfAsync(campId);
            var fileName = $"CampReport_{campId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            
            return File(fileBytes, "application/pdf", fileName);
        }
    }
}
