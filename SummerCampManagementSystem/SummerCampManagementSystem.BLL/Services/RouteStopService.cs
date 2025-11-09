using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.RouteStop;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class RouteStopService : IRouteStopService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        
        public RouteStopService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<RouteStopResponseDto> AddRouteStopAsync(RouteStopRequestDto routeStopRequestDto)
        {
            var route = await _unitOfWork.Routes.GetByIdAsync(routeStopRequestDto.routeId);
            if (route == null)
            {
                throw new KeyNotFoundException($"Route with ID {routeStopRequestDto.routeId} not found.");
            }

            var newRouteStop = _mapper.Map<DAL.Models.RouteStop>(routeStopRequestDto);
            newRouteStop.status = "Active";
            await _unitOfWork.RouteStops.CreateAsync(newRouteStop);
            await _unitOfWork.CommitAsync();

            newRouteStop.route = route;

            return _mapper.Map<RouteStopResponseDto>(newRouteStop);
        }

        public async Task<bool> DeleteRouteStopAsync(int routeStopId)
        {
            var existingRouteStop = await _unitOfWork.RouteStops.GetByIdAsync(routeStopId);
            if (existingRouteStop == null) throw new KeyNotFoundException($"Route Stop with ID {routeStopId} not found.");

            existingRouteStop.status = "Inactive";

            await _unitOfWork.RouteStops.UpdateAsync(existingRouteStop);
            await _unitOfWork.CommitAsync();

            return true;
        }

        public async Task<IEnumerable<RouteStopResponseDto>> GetRouteStopsByRouteIdAsync(int routeId)
        {
            var route = await _unitOfWork.Routes.GetByIdAsync(routeId);
            if (route == null)
            {
                throw new KeyNotFoundException($"Route with ID {routeId} not found.");
            }

            var routeStops = await _unitOfWork.RouteStops.GetQueryable()
                .Where(rs => rs.routeId == routeId && rs.status == "Active")
                .OrderBy(rs => rs.stopOrder)
                .ToListAsync();

            return _mapper.Map<IEnumerable<RouteStopResponseDto>>(routeStops);
        }

        public async Task<RouteStopResponseDto> UpdateRouteStopAsync(int routeStopId, RouteStopRequestDto routeStopRequestDto)
        {
            var existingRouteStop = await _unitOfWork.RouteStops.GetByIdAsync(routeStopId);

            if (existingRouteStop == null)
            {
                throw new KeyNotFoundException($"Route Stop with ID {routeStopId} not found.");
            }

            _mapper.Map(routeStopRequestDto, existingRouteStop);
            await _unitOfWork.RouteStops.UpdateAsync(existingRouteStop);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<RouteStopResponseDto>(existingRouteStop);
        }
    }
}
