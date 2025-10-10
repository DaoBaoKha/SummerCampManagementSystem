using SummerCampManagementSystem.BLL.DTOs.Requests.Route;
using SummerCampManagementSystem.BLL.DTOs.Responses.Route;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IRouteService
    {
        Task<IEnumerable<RouteResponseDto>> GetAllRoutesAsync();
        Task<RouteResponseDto> GetRouteByIdAsync(int routeId);

        Task<IEnumerable<RouteResponseDto>> GetRoutesByCampIdAsync(int campId);

        Task<RouteResponseDto> CreateRouteAsync(RouteRequestDto routeRequestDto);

        Task<RouteResponseDto> UpdateRouteAsync(int routeId, RouteRequestDto routeRequestDto);

        Task<bool> DeleteRouteAsync(int routeId);
    }
}
