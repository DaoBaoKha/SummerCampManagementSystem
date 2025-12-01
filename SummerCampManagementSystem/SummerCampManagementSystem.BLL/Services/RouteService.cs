using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.Route;
using SummerCampManagementSystem.BLL.DTOs.RouteStop;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class RouteService : IRouteService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public RouteService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<RouteResponseDto> CreateRouteAsync(RouteRequestDto routeRequestDto)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(routeRequestDto.campId);
            if (camp == null)
            {
                throw new KeyNotFoundException($"Camp with ID {routeRequestDto.campId} not found.");
            }

            var newRoute = _mapper.Map<Route>(routeRequestDto); 

            newRoute.status = "Active";

            await _unitOfWork.Routes.CreateAsync(newRoute);
            await _unitOfWork.CommitAsync();

            newRoute.camp = camp;

            return _mapper.Map<RouteResponseDto>(newRoute); 
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
            return await _unitOfWork.Routes.GetQueryable()
                .Include(r => r.camp) 
                .ProjectTo<RouteResponseDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<RouteResponseDto> GetRouteByIdAsync(int routeId)
        {
            var route = await _unitOfWork.Routes.GetQueryable()
                .Include(r => r.camp)
                .FirstOrDefaultAsync(r => r.routeId == routeId);

            if (route == null) throw new KeyNotFoundException($"Route with ID {routeId} not found.");

            return _mapper.Map<RouteResponseDto>(route); 
        }

        public async Task<IEnumerable<RouteResponseDto>> GetRoutesByCampIdAsync(int campId)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId);
            if (camp == null)
            {
                throw new NotFoundException($"Không tìm thấy Camp với ID {campId}.");
            }

            var routes = await GetRoutesWithIncludes()
                .Where(r => r.campId == campId) 
                .ToListAsync();

            return _mapper.Map<IEnumerable<RouteResponseDto>>(routes);
        }


        public async Task<RouteResponseDto> UpdateRouteAsync(int routeId, RouteRequestDto routeRequestDto)
        {
            var existingRoute = await _unitOfWork.Routes.GetByIdAsync(routeId);
            if (existingRoute == null) throw new KeyNotFoundException($"Route with ID {routeId} not found.");

            var camp = await _unitOfWork.Camps.GetByIdAsync(routeRequestDto.campId);

            _mapper.Map(routeRequestDto, existingRoute);

            existingRoute.status = "Active";

            await _unitOfWork.Routes.UpdateAsync(existingRoute);
            await _unitOfWork.CommitAsync();

            existingRoute.camp = camp; 
            return _mapper.Map<RouteResponseDto>(existingRoute); 
        }

        #region Private Methods

        private IQueryable<Route> GetRoutesWithIncludes()
        {
            return _unitOfWork.Routes.GetQueryable()
                .Include(r => r.camp) 
                .Include(r => r.RouteStops); 
        }

        #endregion

    }
}
