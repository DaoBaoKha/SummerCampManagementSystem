using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.Route;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
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
            newRoute.isActive = true;

            await _unitOfWork.Routes.CreateAsync(newRoute);
            await _unitOfWork.CommitAsync();

            newRoute.camp = camp;

            return _mapper.Map<RouteResponseDto>(newRoute); 
        }

        public async Task<List<RouteResponseDto>> CreateRouteCompositeAsync(CreateRouteCompositeRequestDto requestDto)
        {
            // validation camp
            var camp = await _unitOfWork.Camps.GetByIdAsync(requestDto.CampId);
            if (camp == null) throw new NotFoundException($"Camp ID {requestDto.CampId} not found.");

            // take camp location
            if (camp.locationId <= 0)
            {
                throw new BusinessRuleException($"Camp ID {requestDto.CampId} chưa được thiết lập địa điểm (LocationId trống). Vui lòng cập nhật thông tin Trại trước.");
            }

            // take location info
            var campLocation = await _unitOfWork.Locations.GetByIdAsync((int)camp.locationId);

            if (campLocation == null)
            {
                throw new NotFoundException($"Không tìm thấy dữ liệu địa điểm với ID {camp.locationId} (được gán trong Camp).");
            }

            // validate location type
            if (campLocation.locationType != LocationType.Camp.ToString())
            {
                throw new BusinessRuleException($"Địa điểm gán cho Camp (ID: {camp.locationId}) không hợp lệ. Yêu cầu LocationType phải là 'Camp', hiện tại là '{campLocation.locationType}'.");
            }

            var createdRoutes = new List<Route>();

            // create pickup route
            var forwardRoute = new Route
            {
                campId = requestDto.CampId,
                routeName = requestDto.RouteName,
                routeType = !string.IsNullOrWhiteSpace(requestDto.RouteType) ? requestDto.RouteType : "PickUp",
                estimateDuration = requestDto.EstimateDuration,
                status = "Active",
                isActive = true,
                RouteStops = new List<RouteStop>()
            };

            var sortedStopsInput = requestDto.RouteStops.OrderBy(s => s.stopOrder).ToList();
            int currentOrder = 1;

            // logic pickup
            // stop point and then camp
            if (forwardRoute.routeType == "PickUp")
            {
                // add stop points
                for (int i = 0; i < sortedStopsInput.Count; i++)
                {
                    forwardRoute.RouteStops.Add(new RouteStop
                    {
                        locationId = sortedStopsInput[i].locationId,
                        stopOrder = currentOrder++,
                        estimatedTime = sortedStopsInput[i].estimatedTime,
                        status = "Active"
                    });
                }

                // auto add camp location at the end
                forwardRoute.RouteStops.Add(new RouteStop
                {
                    locationId = campLocation.locationId,
                    stopOrder = currentOrder,
                    estimatedTime = requestDto.EstimateDuration,
                    status = "Active"
                });
            }
            else //case create dropoff
            {
                // auto add camp location at the beginning
                forwardRoute.RouteStops.Add(new RouteStop
                {
                    locationId = campLocation.locationId,
                    stopOrder = currentOrder++,
                    estimatedTime = 0,
                    status = "Active"
                });

                // add stop points
                for (int i = 0; i < sortedStopsInput.Count; i++)
                {
                    forwardRoute.RouteStops.Add(new RouteStop
                    {
                        locationId = sortedStopsInput[i].locationId,
                        stopOrder = currentOrder++,
                        estimatedTime = sortedStopsInput[i].estimatedTime,
                        status = "Active"
                    });
                }
            }

            await _unitOfWork.Routes.CreateAsync(forwardRoute);
            createdRoutes.Add(forwardRoute);

            // create return route
            if (requestDto.CreateReturnRoute)
            {
                string returnName = !string.IsNullOrWhiteSpace(requestDto.ReturnRouteName)
                    ? requestDto.ReturnRouteName
                    : requestDto.RouteName;

                string returnType = forwardRoute.routeType == "PickUp" ? "DropOff" : "PickUp";

                var returnRoute = new Route
                {
                    campId = requestDto.CampId,
                    routeName = returnName,
                    routeType = returnType,
                    estimateDuration = requestDto.EstimateDuration,
                    status = "Active",
                    isActive = true,
                    RouteStops = new List<RouteStop>()
                };

                currentOrder = 1;

                // camp -> stop points
                if (returnType == "DropOff")
                {
                    // auto add camp location at the beginning
                    returnRoute.RouteStops.Add(new RouteStop
                    {
                        locationId = campLocation.locationId,
                        stopOrder = currentOrder++,
                        estimatedTime = 0,
                        status = "Active"
                    });

                    // reverse stop points
                    var locationsReversed = sortedStopsInput.Select(s => s.locationId).Reverse().ToList();
                    var timesReversed = sortedStopsInput.Select(s => s.estimatedTime).Reverse().ToList();

                    for (int i = 0; i < locationsReversed.Count; i++)
                    {
                        returnRoute.RouteStops.Add(new RouteStop
                        {
                            locationId = locationsReversed[i],
                            stopOrder = currentOrder++,
                            estimatedTime = timesReversed[i], 
                            status = "Active"
                        });
                    }
                }
                // logic pickup
                // stop points -> camp
                else
                {
                    var locationsReversed = sortedStopsInput.Select(s => s.locationId).Reverse().ToList();
                    var timesReversed = sortedStopsInput.Select(s => s.estimatedTime).Reverse().ToList();

                    for (int i = 0; i < locationsReversed.Count; i++)
                    {
                        returnRoute.RouteStops.Add(new RouteStop
                        {
                            locationId = locationsReversed[i],
                            stopOrder = currentOrder++,
                            estimatedTime = timesReversed[i], 
                            status = "Active"
                        });
                    }

                    // auto add camp location at the end
                    returnRoute.RouteStops.Add(new RouteStop
                    {
                        locationId = campLocation.locationId,
                        stopOrder = currentOrder,
                        estimatedTime = requestDto.EstimateDuration, 
                        status = "Active"
                    });
                }

                await _unitOfWork.Routes.CreateAsync(returnRoute);
                createdRoutes.Add(returnRoute);
            }

            await _unitOfWork.CommitAsync();

            foreach (var r in createdRoutes) { r.camp = camp; }

            return _mapper.Map<List<RouteResponseDto>>(createdRoutes);
        }

        public async Task<bool> DeleteRouteAsync(int routeId)
        {
            var existingRoute = await _unitOfWork.Routes.GetByIdAsync(routeId);
            if (existingRoute == null) throw new KeyNotFoundException($"Route with ID {routeId} not found.");

            // check if route is being used in any TransportSchedules
            var isUsedInTransportSchedule = await _unitOfWork.TransportSchedules.GetQueryable()
                .Where(ts => ts.routeId == routeId)
                .AnyAsync();

            if (isUsedInTransportSchedule)
            {
                throw new BusinessRuleException("Không thể xóa tuyến đường vì đang được sử dụng trong lịch vận chuyển.");
            }

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
