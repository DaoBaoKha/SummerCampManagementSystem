using SummerCampManagementSystem.BLL.DTOs.CamperTransport;
using SummerCampManagementSystem.BLL.DTOs.TransportSchedule;
using SummerCampManagementSystem.Core.Enums;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface ITransportScheduleService
    {
        Task<TransportScheduleResponseDto> CreateScheduleAsync(TransportScheduleRequestDto requestDto);
        Task<TransportScheduleResponseDto> GetScheduleByIdAsync(int id);
        Task<IEnumerable<TransportScheduleResponseDto>> GetDriverSchedulesAsync();
        Task<TransportScheduleResponseDto> UpdateScheduleAsync(int id, TransportScheduleRequestDto requestDto);
        Task<bool> DeleteScheduleAsync(int id);
        Task<IEnumerable<TransportScheduleResponseDto>> GetAllSchedulesAsync();
        Task<IEnumerable<TransportScheduleResponseDto>> SearchAsync(TransportScheduleSearchDto searchDto); 
        Task<TransportScheduleResponseDto> UpdateActualTimeAsync(int id, TimeOnly? actualStartTime, TimeOnly? actualEndTime);
        Task<TransportScheduleResponseDto> UpdateScheduleStatusAsync(int id, TransportScheduleStatus desiredStatus, string? cancelReason = null);
        Task<IEnumerable<TransportScheduleResponseDto>> GetSchedulesByCamperIdAsync(int camperId);
        Task<IEnumerable<CamperInScheduleResponseDto>> GetCampersInScheduleAsync(int scheduleId);
    }
}
