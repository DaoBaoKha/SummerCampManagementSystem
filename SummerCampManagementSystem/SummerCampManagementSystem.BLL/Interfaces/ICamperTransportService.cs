using SummerCampManagementSystem.BLL.DTOs.CamperTransport;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface ICamperTransportService
    {
        Task<IEnumerable<CamperTransportResponseDto>> GetCampersByScheduleIdAsync(int transportScheduleId);
        Task<CamperTransportResponseDto> UpdateStatusAsync(int camperTransportId, CamperTransportUpdateDto updateDto);

        Task<bool> GenerateCamperListForScheduleAsync(int transportScheduleId);
    }
}
