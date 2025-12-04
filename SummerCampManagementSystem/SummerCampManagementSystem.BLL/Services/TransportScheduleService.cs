using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.TransportSchedule;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class TransportScheduleService : ITransportScheduleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IUserContextService _userContextService;

        public TransportScheduleService(IUnitOfWork unitOfWork, IMapper mapper, IUserContextService userContextService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userContextService = userContextService;
        }

        public async Task<TransportScheduleResponseDto> CreateScheduleAsync(TransportScheduleRequestDto requestDto)
        {
            // basic validation
            checkTimeBounds(requestDto.StartTime, requestDto.EndTime);
            checkTransportType(requestDto.TransportType);
            checkScheduleDate(requestDto.Date);

            requestDto.StartTime = requestDto.StartTime.ToUtcForStorageTime();
            requestDto.EndTime = requestDto.EndTime.ToUtcForStorageTime();

            await CheckForeignKeyExistence(requestDto);

            await CheckScheduleConflicts(
                requestDto.DriverId,
                requestDto.VehicleId,
                requestDto.Date,
                requestDto.StartTime,
                requestDto.EndTime
            );

            var scheduleEntity = _mapper.Map<TransportSchedule>(requestDto);
            scheduleEntity.status = TransportScheduleStatus.Draft.ToString();

            await _unitOfWork.TransportSchedules.CreateAsync(scheduleEntity);
            await _unitOfWork.CommitAsync();

            var createdSchedule = await GetSchedulesWithIncludes()
                                         .FirstAsync(s => s.transportScheduleId == scheduleEntity.transportScheduleId);

            return _mapper.Map<TransportScheduleResponseDto>(createdSchedule);
        }

        public async Task<TransportScheduleResponseDto> GetScheduleByIdAsync(int id)
        {
            var schedule = await GetSchedulesWithIncludes()
                                 .FirstOrDefaultAsync(s => s.transportScheduleId == id)
                                 ?? throw new NotFoundException($"Transport Schedule ID {id} not found.");

            return _mapper.Map<TransportScheduleResponseDto>(schedule);
        }

        public async Task<IEnumerable<TransportScheduleResponseDto>> GetAllSchedulesAsync()
        {
            var schedules = await GetSchedulesWithIncludes().ToListAsync();
            return _mapper.Map<IEnumerable<TransportScheduleResponseDto>>(schedules);
        }

        public async Task<IEnumerable<TransportScheduleResponseDto>> GetDriverSchedulesAsync()
        {
            var currentUserId = _userContextService.GetCurrentUserId();

            // find driverId by userId
            var driverEntity = await _unitOfWork.Drivers.GetQueryable()
                                                    .Include(d => d.user) 
                                                    .FirstOrDefaultAsync(d => d.user.userId == currentUserId);

            if (driverEntity == null)
            {
                return Enumerable.Empty<TransportScheduleResponseDto>();
            }

            var schedules = await GetSchedulesWithIncludes()
                                .Where(s => s.driverId == driverEntity.driverId)
                                .ToListAsync();

            return _mapper.Map<IEnumerable<TransportScheduleResponseDto>>(schedules);
        }

        public async Task<IEnumerable<TransportScheduleResponseDto>> SearchAsync(TransportScheduleSearchDto searchDto)
        {
            IQueryable<TransportSchedule> query = GetSchedulesWithIncludes();

            // make sure searchDto is not null
            if (searchDto == null)
            {
                searchDto = new TransportScheduleSearchDto();
            }

            if (searchDto.CampId.HasValue && searchDto.CampId.Value > 0)
            {
                query = query.Where(t => t.campId == searchDto.CampId.Value);
            }

            if (searchDto.VehicleId.HasValue && searchDto.VehicleId.Value > 0)
            {
                query = query.Where(t => t.vehicleId == searchDto.VehicleId.Value);
            }

            if (searchDto.DriverId.HasValue && searchDto.DriverId.Value > 0)
            {
                query = query.Where(t => t.driverId == searchDto.DriverId.Value);
            }

            if (searchDto.RouteId.HasValue && searchDto.RouteId.Value > 0)
            {
                query = query.Where(t => t.routeId == searchDto.RouteId.Value);
            }

            if (searchDto.Date.HasValue)
            {
                query = query.Where(t => t.date == searchDto.Date.Value);
            }
            else
            {
                if (searchDto.StartDate.HasValue)
                {
                    query = query.Where(t => t.date >= searchDto.StartDate.Value);
                }
                if (searchDto.EndDate.HasValue)
                {
                    query = query.Where(t => t.date <= searchDto.EndDate.Value);
                }
            }

            if (!string.IsNullOrWhiteSpace(searchDto.Status))
            {
                // use toLower to make case insensitive
                string statusLower = searchDto.Status.Trim().ToLower();
                query = query.Where(t => t.status.ToLower().Contains(statusLower));
            }

            // if no filters provided => get all

            var entities = await query.ToListAsync();

            return _mapper.Map<IEnumerable<TransportScheduleResponseDto>>(entities);
        }

        public async Task<TransportScheduleResponseDto> UpdateScheduleAsync(int id, TransportScheduleRequestDto requestDto)
        {
            var existingSchedule = await _unitOfWork.TransportSchedules.GetByIdAsync(id)
                ?? throw new NotFoundException($"Không tìm thấy lịch trình vận chuyển với ID {id}.");

            checkTimeBounds(requestDto.StartTime, requestDto.EndTime);
            checkTransportType(requestDto.TransportType);
            checkScheduleDate(requestDto.Date);

            // validation fk
            if (existingSchedule.routeId != requestDto.RouteId || existingSchedule.driverId != requestDto.DriverId 
                || existingSchedule.vehicleId != requestDto.VehicleId || existingSchedule.campId != requestDto.CampId)
            {
                await CheckForeignKeyExistence(requestDto);
            }

            // only update when status before InProgress
            if (existingSchedule.status == TransportScheduleStatus.InProgress.ToString() || existingSchedule.status == TransportScheduleStatus.Completed.ToString()
                || existingSchedule.status == TransportScheduleStatus.Canceled.ToString())
            {
                throw new BusinessRuleException($"Không thể chỉnh sửa dữ liệu lịch trình khi trạng thái là {existingSchedule.status}. Chỉ có thể chỉnh sửa khi lịch trình đang ở trạng thái Draft, NotYet và Rejected.");
            }

            requestDto.StartTime = requestDto.StartTime.ToUtcForStorageTime();
            requestDto.EndTime = requestDto.EndTime.ToUtcForStorageTime();

            // validation conflict except current schedule
            await CheckScheduleConflicts(
                requestDto.DriverId,
                requestDto.VehicleId,
                requestDto.Date,
                requestDto.StartTime,
                requestDto.EndTime,
                id 
            );

            _mapper.Map(requestDto, existingSchedule);

            // if update from Rejected -> status = Draft
            if (existingSchedule.status == TransportScheduleStatus.Rejected.ToString())
            {
                existingSchedule.status = TransportScheduleStatus.Draft.ToString();
            }


            await _unitOfWork.TransportSchedules.UpdateAsync(existingSchedule);
            await _unitOfWork.CommitAsync();

            var updatedSchedule = await GetSchedulesWithIncludes()
                                        .FirstAsync(s => s.transportScheduleId == id);

            return _mapper.Map<TransportScheduleResponseDto>(updatedSchedule);
        }

        public async Task<TransportScheduleResponseDto> UpdateScheduleStatusAsync(int id, TransportScheduleStatus desiredStatus, string? cancelReason = null)
        {
            var existingSchedule = await _unitOfWork.TransportSchedules.GetByIdAsync(id)
                ?? throw new NotFoundException($"Transport Schedule ID {id} not found.");

            // camp check manually
            switch (desiredStatus)
            {
                case TransportScheduleStatus.NotYet:
                    // admin approve
                    CheckAndSetStatusTransition(existingSchedule, desiredStatus, TransportScheduleStatus.Draft);
                    break;

                case TransportScheduleStatus.Rejected:
                    // admin reject
                    CheckAndSetStatusTransition(existingSchedule, desiredStatus, TransportScheduleStatus.Draft);
                    break;

                case TransportScheduleStatus.Canceled:
                    // admin cancels (NotYet -> Canceled) or (InProgress -> Canceled)
                    CheckAndSetStatusTransition(existingSchedule, desiredStatus, TransportScheduleStatus.NotYet, TransportScheduleStatus.InProgress);
                    existingSchedule.cancelReasons = cancelReason;

                    // when cancel -> reset actual time
                    existingSchedule.actualStartTime = null;
                    existingSchedule.actualEndTime = null;
                    break;

                case TransportScheduleStatus.Draft:
                    // Draft only set when create new or change from Rejected using updateScheduleAsync
                    throw new BusinessRuleException($"Không thể chuyển trực tiếp sang trạng thái '{desiredStatus}'. Trạng thái này không được chuyển thủ công.");

                case TransportScheduleStatus.InProgress:

                case TransportScheduleStatus.Completed:
                    // InProgress and Completed auto update from UpdateActualTimeAsync
                    throw new BusinessRuleException($"Không thể chuyển thủ công sang trạng thái '{desiredStatus}'. Trạng thái này được xác định tự động.");

                default:
                    throw new BusinessRuleException($"Trạng thái '{desiredStatus}' không hợp lệ cho việc chuyển đổi thủ công.");
            }

            await _unitOfWork.TransportSchedules.UpdateAsync(existingSchedule);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<TransportScheduleResponseDto>(await GetSchedulesWithIncludes().FirstAsync(s => s.transportScheduleId == id));
        }

        public async Task<TransportScheduleResponseDto> UpdateActualTimeAsync(int id, TimeOnly? actualStartTime, TimeOnly? actualEndTime)
        {
            var existingSchedule = await _unitOfWork.TransportSchedules.GetByIdAsync(id)
                ?? throw new NotFoundException($"Transport Schedule ID {id} not found.");

            TimeOnly? utcStartTime = actualStartTime.ToUtcForStorageTime();
            TimeOnly? utcEndTime = actualEndTime.ToUtcForStorageTime();

            ApplyActualTimeAndDetermineStatus(existingSchedule, actualStartTime, actualEndTime);

            // auto change status of all camper in transportSchedule to completed when update endTime
            if (existingSchedule.status == TransportScheduleStatus.Completed.ToString() && existingSchedule.actualEndTime.HasValue)
            {
                // find camperTransport status = Onboard
                var campersOnBoard = await _unitOfWork.CamperTransports.GetQueryable()
                    .Where(ct => ct.transportScheduleId == id &&
                                 ct.status == CamperTransportStatus.Onboard.ToString())
                    .ToListAsync();

                var now = TimezoneHelper.GetVietnamNow();

                foreach (var ct in campersOnBoard)
                {
                    ct.status = CamperTransportStatus.Completed.ToString();
                    ct.checkOutTime = now;

                    await _unitOfWork.CamperTransports.UpdateAsync(ct);
                }
            }

            await _unitOfWork.TransportSchedules.UpdateAsync(existingSchedule);
            await _unitOfWork.CommitAsync();

            var updatedSchedule = await GetSchedulesWithIncludes().FirstAsync(s => s.transportScheduleId == id);

            return _mapper.Map<TransportScheduleResponseDto>(updatedSchedule);
        }


        public async Task<bool> DeleteScheduleAsync(int id)
        {
            var scheduleToDelete = await _unitOfWork.TransportSchedules.GetByIdAsync(id)
                ?? throw new NotFoundException($"Transport Schedule ID {id} not found.");

            // only allow delete when status = Draft or Rejected
            if (scheduleToDelete.status != TransportScheduleStatus.Draft.ToString() &&
                scheduleToDelete.status != TransportScheduleStatus.Rejected.ToString())
            {
                throw new BusinessRuleException($"Không thể xóa lịch trình khi trạng thái là {scheduleToDelete.status}. Chỉ có thể xóa lịch trình ở trạng thái Draft hoặc Rejected.");
            }

            scheduleToDelete.status = TransportScheduleStatus.Canceled.ToString();
            await _unitOfWork.TransportSchedules.UpdateAsync(scheduleToDelete);
            await _unitOfWork.CommitAsync();

            return true;
        }

        #region Private Methods

        private IQueryable<TransportSchedule> GetSchedulesWithIncludes()
        {
            return _unitOfWork.TransportSchedules.GetQueryable()
                .Include(s => s.camp)
                .Include(s => s.route)
                .Include(s => s.vehicle)
                .Include(s => s.driver).ThenInclude(d => d.user);
        }

        private void checkTimeBounds(TimeOnly startTime, TimeOnly endTime)
        {
            // check if start time is before end time
            if (startTime >= endTime)
            {
                throw new BusinessRuleException("Thời gian bắt đầu phải sớm hơn thời gian kết thúc.");
            }
        }

        private void checkScheduleDate(DateOnly scheduleDate)
        {
            // check if the schedule date is in the past
            if (scheduleDate < DateOnly.FromDateTime(DateTime.UtcNow.Date))
            {
                throw new BusinessRuleException("Không thể tạo hoặc cập nhật lịch trình cho ngày đã qua.");
            }
        }

        private void checkTransportType(string? transportType) 
        {
            // get all enums
            var validTypes = Enum.GetNames(typeof(TransportScheduleType));

            if (string.IsNullOrWhiteSpace(transportType) || !validTypes.Contains(transportType))
            {
                var allowedTypes = string.Join(" hoặc ", validTypes);
                throw new BusinessRuleException($"Loại chuyến đi (TransportType) phải được xác định rõ là '{TransportScheduleType.PickUp}' hoặc '{TransportScheduleType.DropOff}'.");
            }
        }

        private void CheckAndSetStatusTransition(TransportSchedule existingSchedule, TransportScheduleStatus newStatus, params TransportScheduleStatus[] allowedCurrentStatuses)
        {
            var currentStatus = existingSchedule.status;

            // state flow check
            if (allowedCurrentStatuses.Length > 0 && !allowedCurrentStatuses.Any(s => s.ToString() == currentStatus))
            {
                var allowedStatusNames = string.Join(", ", allowedCurrentStatuses.Select(s => $"'{s}'"));
                throw new BusinessRuleException($"Không thể chuyển trạng thái từ '{currentStatus}' sang '{newStatus}'. Chỉ cho phép chuyển đổi khi trạng thái hiện tại là: {allowedStatusNames}.");
            }

            existingSchedule.status = newStatus.ToString();
        }

        private async Task CheckScheduleConflicts(int driverId, int vehicleId, DateOnly date, TimeOnly startTime, TimeOnly endTime, int? excludeScheduleId = null)
        {
            var activeStatuses = new[] {
                TransportScheduleStatus.Draft.ToString(),
                TransportScheduleStatus.NotYet.ToString(),
                TransportScheduleStatus.InProgress.ToString()
            };

            var query = _unitOfWork.TransportSchedules.GetQueryable()
                .Where(s => s.date == date && activeStatuses.Contains(s.status));

            if (excludeScheduleId.HasValue)
            {
                query = query.Where(s => s.transportScheduleId != excludeScheduleId.Value);
            }

            // check driver conflict
            var driverConflict = await query
                .Where(s => s.driverId == driverId)
                .Where(s => s.endTime > startTime && s.startTime < endTime)
                .FirstOrDefaultAsync();

            if (driverConflict != null)
            {
                throw new BusinessRuleException($"Tài xế ID {driverId} đã có lịch trình chồng chéo (ID: {driverConflict.transportScheduleId}) từ {driverConflict.startTime} đến {driverConflict.endTime} vào ngày {date}.");
            }

            // check vehicle conflict
            var vehicleConflict = await query
                .Where(s => s.vehicleId == vehicleId)
                .Where(s => s.endTime > startTime && s.startTime < endTime)
                .FirstOrDefaultAsync();

            if (vehicleConflict != null)
            {
                throw new BusinessRuleException($"Xe ID {vehicleId} đã được sử dụng trong lịch trình chồng chéo (ID: {vehicleConflict.transportScheduleId}) từ {vehicleConflict.startTime} đến {vehicleConflict.endTime} vào ngày {date}.");
            }
        }

        private async Task CheckForeignKeyExistence(TransportScheduleRequestDto requestDto)
        {
            // validate camp
            var camp = await _unitOfWork.Camps.GetByIdAsync(requestDto.CampId);
            if (camp == null)
                throw new NotFoundException($"Không tìm thấy Camp ID {requestDto.CampId}.");

            // check camp date
            if (!camp.startDate.HasValue || !camp.endDate.HasValue)
            {
                throw new BusinessRuleException($"Camp ID {requestDto.CampId} chưa được thiết lập ngày bắt đầu hoặc ngày kết thúc, không thể tạo lịch trình.");
            }

            // use .value to get DateTime value
            var campStartDate = DateOnly.FromDateTime(camp.startDate.Value);
            var campEndDate = DateOnly.FromDateTime(camp.endDate.Value);

            if (requestDto.TransportType == TransportScheduleType.PickUp.ToString())
            {
                // pickUp must be before startDate
                if (requestDto.Date > campStartDate)
                {
                    throw new BusinessRuleException($"Lịch trình 'PickUp' (Đón đi) phải diễn ra trước hoặc vào ngày bắt đầu trại ({campStartDate}). Ngày bạn chọn: {requestDto.Date}");
                }
            }
            else if (requestDto.TransportType == TransportScheduleType.DropOff.ToString())
            {
                // dropOff must be after endDate
                if (requestDto.Date < campEndDate)
                {
                    throw new BusinessRuleException($"Lịch trình 'DropOff' (Đưa về) phải diễn ra sau hoặc vào ngày kết thúc trại ({campEndDate}). Ngày bạn chọn: {requestDto.Date}");
                }
            }

            // check route existence and status
            var route = await _unitOfWork.Routes.GetByIdAsync(requestDto.RouteId);
            if (route == null) throw new NotFoundException($"Không tìm thấy Route ID {requestDto.RouteId}.");

            // check if route active
            if (route.isActive == false) throw new BusinessRuleException($"Route ID {requestDto.RouteId} hiện không hoạt động.");

            // check route and camp consistency
            if (route.campId != requestDto.CampId)
            {
                throw new BusinessRuleException($"Tuyến đường (Route) được chọn không thuộc về Trại (Camp) này. Route ID {requestDto.RouteId} thuộc Camp {route.campId}, nhưng bạn đang gán cho Camp {requestDto.CampId}.");
            }

            // check driver existence and status 
            var driver = await _unitOfWork.Drivers.GetByIdAsync(requestDto.DriverId);

            if (driver == null) throw new NotFoundException($"Không tìm thấy Driver ID {requestDto.DriverId}.");

            // only approved driver can be add
            if (driver.status != DriverStatus.Approved.ToString())
                throw new BusinessRuleException($"Tài xế ID {requestDto.DriverId} không ở trạng thái Approved (trạng thái hiện tại: {driver.status}).");

            // check vehicle existence and status
            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(requestDto.VehicleId);

            if (vehicle == null)
                throw new NotFoundException($"Không tìm thấy Vehicle ID {requestDto.VehicleId}.");
            
            // check if vehicle active
            if (vehicle.status != VehicleStatus.Active.ToString())
                throw new BusinessRuleException($"Xe ID {requestDto.VehicleId} không sẵn sàng (trạng thái hiện tại: {vehicle.status}).");
        }

        private void ApplyActualTimeAndDetermineStatus(TransportSchedule existingSchedule, TimeOnly? actualStartTime, TimeOnly? actualEndTime)
        {
            var currentStatus = existingSchedule.status;

            // current status must be NotYet or InProgress
            if (currentStatus != TransportScheduleStatus.NotYet.ToString() &&
                currentStatus != TransportScheduleStatus.InProgress.ToString())
            {
                throw new BusinessRuleException($"Không thể cập nhật thời gian thực tế khi trạng thái là {currentStatus}. Chỉ cho phép cập nhật khi trạng thái là '{TransportScheduleStatus.NotYet}' hoặc '{TransportScheduleStatus.InProgress}'.");
            }

            // if no ActualStartTime & status < InProgress & ActualEndTime has value -> unallowed
            if (!existingSchedule.actualStartTime.HasValue && actualEndTime.HasValue)
            {
                throw new BusinessRuleException($"Không thể ghi nhận giờ kết thúc thực tế. Lịch trình chưa có giờ bắt đầu thực tế ({TransportScheduleStatus.InProgress} chưa được kích hoạt).");
            }

            // update ActualEndTime if has new value
            if (actualStartTime.HasValue)
            {
                existingSchedule.actualStartTime = actualStartTime;
            }

            // only update if has value
            if (actualEndTime.HasValue)
            {
                existingSchedule.actualEndTime = actualEndTime;
            }
            // if ActualEndTime = null -> remain unchanged


            // new status
            if (existingSchedule.actualEndTime.HasValue)
            {
                // auto completed if has ActualEndTime
                existingSchedule.status = TransportScheduleStatus.Completed.ToString();
            }
            else if (existingSchedule.actualStartTime.HasValue)
            {
                // if has ActualStartTime but no ActualEndTime -> InProgress
                existingSchedule.status = TransportScheduleStatus.InProgress.ToString();
            }
            else
            {
                // if both null -> NotYet
                existingSchedule.status = TransportScheduleStatus.NotYet.ToString();
            }
        }

        #endregion
    }
}