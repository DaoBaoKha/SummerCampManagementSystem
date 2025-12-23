using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.RouteStop;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class RouteStopService : IRouteStopService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IRouteStopRepository _routeStopRepository;

        public RouteStopService(IUnitOfWork unitOfWork, IMapper mapper, IRouteStopRepository routeStopRepository)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _routeStopRepository = routeStopRepository;
        }

        #region Public Methods

        public async Task<RouteStopResponseDto> AddRouteStopAsync(RouteStopRequestDto dto)
        {
            // validate BR before add
            await ValidateRouteStopAsync(dto);

            var newStop = _mapper.Map<RouteStop>(dto);
            newStop.status = "Active";

            try
            {
                await _unitOfWork.RouteStops.CreateAsync(newStop);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                throw new SystemException("Lỗi hệ thống khi tạo điểm dừng: " + ex.Message);
            }

            var routeStop = await GetRouteStopByIdAsync(newStop.routeStopId);

            return _mapper.Map<RouteStopResponseDto>(routeStop);
        }

        public async Task<RouteStopResponseDto> UpdateRouteStopAsync(int id, RouteStopRequestDto dto)
        {
            var existing = await _unitOfWork.RouteStops.GetByIdAsync(id);
            if (existing == null)
                throw new NotFoundException($"Không tìm thấy điểm dừng có ID {id}.");

            // validate BR before add
            await ValidateRouteStopAsync(dto, id);

            _mapper.Map(dto, existing);

            try
            {
                await _unitOfWork.RouteStops.UpdateAsync(existing);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                throw new SystemException("Lỗi hệ thống khi cập nhật điểm dừng: " + ex.Message);
            }

            var routeStop = await GetRouteStopByIdAsync(existing.routeStopId);


            return _mapper.Map<RouteStopResponseDto>(routeStop);
        }

        public async Task<bool> DeleteRouteStopAsync(int id)
        {
            var existing = await _unitOfWork.RouteStops.GetByIdAsync(id);
            if (existing == null)
                throw new NotFoundException($"Không tìm thấy điểm dừng có ID {id}.");

            // check if the route containing this RouteStop is in active TransportSchedules (NotYet or InProgress)
            var isRouteUsedInActiveTransportSchedule = await _unitOfWork.TransportSchedules.GetQueryable()
                .Where(ts => ts.routeId == existing.routeId 
                    && (ts.status == TransportScheduleStatus.NotYet.ToString() 
                        || ts.status == TransportScheduleStatus.InProgress.ToString()))
                .AnyAsync();

            if (isRouteUsedInActiveTransportSchedule)
            {
                throw new BusinessRuleException("Không thể xóa điểm dừng vì tuyến đường đang được sử dụng trong lịch vận chuyển đang hoạt động.");
            }

            // check if any CamperTransport is using this stop location
            var isCamperTransportUsing = await _unitOfWork.CamperTransports.GetQueryable()
                .Where(ct => ct.stopLocationId == existing.locationId)
                .AnyAsync();

            if (isCamperTransportUsing)
            {
                throw new BusinessRuleException("Không thể xóa điểm dừng vì đang có camper sử dụng điểm dừng này.");
            }

            existing.status = "Inactive";

            try
            {
                await _unitOfWork.RouteStops.UpdateAsync(existing);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                throw new SystemException("Lỗi hệ thống khi xóa điểm dừng: " + ex.Message);
            }

            return true;
        }

        public async Task<IEnumerable<RouteStopResponseDto>> GetAllRouteStopsAsync()
        {
            var routeStops = await _routeStopRepository.GetRouteStopsWithIncludes()
                .Where(rs => rs.status == "Active") // only return active route stops
                .ToListAsync();
            return _mapper.Map<IEnumerable<RouteStopResponseDto>>(routeStops);
        }

        public async Task<RouteStopResponseDto> GetRouteStopByIdAsync(int routeStopId)
        {
            var routeStop = await _routeStopRepository.GetRouteStopsWithIncludes().FirstOrDefaultAsync(r => r.routeStopId == routeStopId)
                ?? throw new NotFoundException($"Không tìm thấy điểm dừng có ID {routeStopId}.");

            return _mapper.Map<RouteStopResponseDto>(routeStop);
        }

        public async Task<IEnumerable<RouteStopResponseDto>> GetRouteStopsByRouteIdAsync(int routeId)
        {
            var route = await _unitOfWork.Routes.GetByIdAsync(routeId);
            if (route == null)
                throw new NotFoundException($"Không tìm thấy tuyến đường có ID {routeId}.");

            var routeStops = await _routeStopRepository.GetRouteStopsWithIncludes()
                .Where(rs => rs.routeId == routeId && rs.status == "Active")
                .OrderBy(rs => rs.stopOrder)
                .ToListAsync();

            return _mapper.Map<IEnumerable<RouteStopResponseDto>>(routeStops);
        }

        #endregion

        #region Private Methods

        private async Task ValidateRouteStopAsync(RouteStopRequestDto dto, int? routeStopId = null)
        {
            // stopOrder > 0
            if (dto.stopOrder <= 0)
                throw new BusinessRuleException("Thứ tự điểm dừng phải lớn hơn 0.");

            // stopOrder not duplicated 
            var stopOrderExists = await _unitOfWork.RouteStops.GetQueryable()
                .AnyAsync(rs => rs.routeId == dto.routeId
                        && rs.stopOrder == dto.stopOrder
                        && (routeStopId == null || rs.routeStopId != routeStopId)
                        && rs.status == "Active");
                    if (stopOrderExists) throw new BusinessRuleException($"Thứ tự điểm dừng {dto.stopOrder} đã tồn tại trong Route {dto.routeId}."); 

            // estimatedTime >= 0 
            if (dto.estimatedTime.HasValue && dto.estimatedTime < 0)
                throw new BusinessRuleException("Thời gian ước tính phải lớn hơn hoặc bằng 0.");

            // route check
            var route = await _unitOfWork.Routes.GetByIdAsync(dto.routeId);
            if (route == null)
                throw new NotFoundException($"Route với ID {dto.routeId} không tồn tại."); 

            // location check
            var location = await _unitOfWork.Locations.GetByIdAsync(dto.locationId);
            if (location == null)
                throw new NotFoundException($"Location với ID {dto.locationId} không tồn tại."); 

        }

        #endregion
    }
}
