using SummerCampManagementSystem.BLL.DTOs.RouteStop;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IRouteStopService
    {
        Task<IEnumerable<RouteStopResponseDto>> GetAllRouteStopsAsync();
        Task<IEnumerable<RouteStopResponseDto>> GetRouteStopsByRouteIdAsync(int routeId);
        Task<RouteStopResponseDto> GetRouteStopByIdAsync(int routeStopId);
        Task<RouteStopResponseDto> AddRouteStopAsync(RouteStopRequestDto routeStopRequestDto);
        Task<RouteStopResponseDto> UpdateRouteStopAsync(int routeStopId, RouteStopRequestDto routeStopRequestDto);
        Task<bool> DeleteRouteStopAsync(int routeStopId);
    }
}
