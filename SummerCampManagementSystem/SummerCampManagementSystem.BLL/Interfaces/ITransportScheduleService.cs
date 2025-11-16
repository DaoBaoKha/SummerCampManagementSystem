using SummerCampManagementSystem.BLL.DTOs.TransportSchedule;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface ITransportScheduleService
    {
        Task<TransportScheduleResponseDto> CreateScheduleAsync(TransportScheduleRequestDto requestDto);
        Task<TransportScheduleResponseDto> GetScheduleByIdAsync(int id);
        Task<TransportScheduleResponseDto> UpdateScheduleAsync(int id, TransportScheduleRequestDto requestDto);
        Task<bool> DeleteScheduleAsync(int id);
        Task<IEnumerable<TransportScheduleResponseDto>> GetSchedulesByRouteAndDateAsync(int routeId, DateOnly date);
        Task<TransportScheduleResponseDto> UpdateActualTimeAsync(int id, TimeOnly? actualStartTime, TimeOnly? actualEndTime);
    }
}
