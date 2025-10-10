using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.Requests.Route;
using SummerCampManagementSystem.BLL.DTOs.Responses.Route;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class RouteService : IRouteService
    {
        private readonly IUnitOfWork _unitOfWork;

        public RouteService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<RouteResponseDto> CreateRouteAsync(RouteRequestDto routeRequestDto)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(routeRequestDto.campId);
            if (camp == null)
            {
                throw new KeyNotFoundException($"Camp with ID {routeRequestDto.campId} not found.");
            }

            var newRoute = new Route
            {
                campId = routeRequestDto.campId,
                routeName = routeRequestDto.routeName,
                status = "Active",
            };

            await _unitOfWork.Routes.CreateAsync(newRoute);
            await _unitOfWork.CommitAsync();

            newRoute.camp = camp; //include camp details in the response

            return MapToResponseDto(newRoute);
        }

        public async Task<bool> DeleteRouteAsync(int routeId)
        {
            var existingRoute = await _unitOfWork.Routes.GetByIdAsync(routeId);
            if (existingRoute == null) throw new KeyNotFoundException($"Route with ID {routeId} not found.");

            await _unitOfWork.Routes.RemoveAsync(existingRoute);
            await _unitOfWork.CommitAsync();

            return true;
        }

        public async Task<IEnumerable<RouteResponseDto>> GetAllRoutesAsync()
        {
            var routes = await _unitOfWork.Routes.GetQueryable()
                .Include(r => r.camp)
                .ToListAsync();

            return routes.Select(MapToResponseDto);
        }

        public async Task<RouteResponseDto> GetRouteByIdAsync(int routeId)
        {
            var route = await _unitOfWork.Routes.GetQueryable()
                .Include(r => r.camp)
                .FirstOrDefaultAsync(r => r.routeId == routeId);

            if (route == null) throw new KeyNotFoundException($"Route with ID {routeId} not found.");

            return MapToResponseDto(route);
        }

        public async Task<IEnumerable<RouteResponseDto>> GetRoutesByCampIdAsync(int campId)
        {
            var route = await _unitOfWork.Routes.GetQueryable()
                .Include(r => r.camp)
                .Where(r => r.campId == campId)
                .ToListAsync();

            return route.Select(MapToResponseDto);
        }

        public async Task<RouteResponseDto> UpdateRouteAsync(int routeId, RouteRequestDto routeRequestDto)
        {
            var existingRoute = await _unitOfWork.Routes.GetByIdAsync(routeId);
            if (existingRoute == null) throw new KeyNotFoundException($"Route with ID {routeId} not found.");

            var camp = await _unitOfWork.Camps.GetByIdAsync(routeRequestDto.campId);
            existingRoute.routeName = routeRequestDto.routeName;
            existingRoute.campId = routeRequestDto.campId;
            existingRoute.status = "Active";

            await _unitOfWork.Routes.UpdateAsync(existingRoute);
            await _unitOfWork.CommitAsync();

            return MapToResponseDto(existingRoute);
        }

        //mapping dto to camp entity
        private RouteResponseDto MapToResponseDto(Route route)
        {
            return new RouteResponseDto
            {
                routeId = route.routeId,
                routeName = route.routeName,
                status = route.status,
                campId = (int)route.campId,
                CampName = route.camp != null ? route.camp.name : string.Empty
            };
        }
    }
}
