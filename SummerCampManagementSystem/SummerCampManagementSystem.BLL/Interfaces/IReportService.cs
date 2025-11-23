using SummerCampManagementSystem.BLL.DTOs.Report;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IReportService
    {
        Task<ReportResponseDto> CreateReportAsync(ReportRequestDto reportRequestDto, int staffId);
        Task<ReportResponseDto?> GetReportByIdAsync(int reportId);
        Task<IEnumerable<ReportResponseDto>> GetAllReportsAsync();
        Task<ReportResponseDto?> UpdateReportAsync(int reportId, ReportRequestDto reportRequestDto);
        Task<bool> DeleteReportAsync(int reportId);
    }
}
