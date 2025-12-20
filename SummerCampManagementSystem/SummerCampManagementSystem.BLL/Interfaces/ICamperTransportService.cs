using SummerCampManagementSystem.BLL.DTOs.CamperTransport;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface ICamperTransportService
    {
        Task<IEnumerable<CamperTransportResponseDto>> GetCampersByScheduleIdAsync(int transportScheduleId);
        Task<IEnumerable<CamperTransportResponseDto>> GetActiveCamperTransportsByScheduleIdAsync(int transportScheduleId);
        Task<IEnumerable<CamperTransportResponseDto>> GetAllCamperTransportAsync();
        Task<CamperTransportResponseDto> UpdateStatusAsync(int camperTransportId, CamperTransportUpdateDto updateDto);
        Task<bool> CamperCheckInAsync(CamperTransportAttendanceDto request);
        Task<bool> CamperCheckOutAsync(CamperTransportAttendanceDto request);
        Task<bool> CamperMarkAbsentAsync(CamperTransportAttendanceDto request);
        Task<bool> GenerateCamperListForScheduleAsync(int transportScheduleId);
    }
}
