using SummerCampManagementSystem.BLL.DTOs.Report;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IReportService
    {
        Task<ReportResponseDto> CreateReportAsync(ReportRequestDto reportRequestDto, int staffId);
        Task<ReportResponseDto?> GetReportByIdAsync(int reportId);
        Task<IEnumerable<ReportResponseDto>> GetAllReportsAsync();
        Task<ReportResponseDto?> UpdateReportAsync(int reportId, ReportRequestDto reportRequestDto);
        Task<bool> DeleteReportAsync(int reportId);
        Task<ReportResponseDto> CreateTransportIncidentAsync(TransportIncidentRequestDto dto, int staffId);
        Task<ReportResponseDto> CreateEarlyCheckoutReportAsync(EarlyCheckoutRequestDto dto, int staffId);
        Task<ReportResponseDto> CreateIncidentTicketAsync(IncidentTicketRequestDto dto, int staffId);
    }
}
