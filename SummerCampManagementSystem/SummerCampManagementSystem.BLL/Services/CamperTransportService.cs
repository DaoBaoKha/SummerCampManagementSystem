using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.CamperTransport;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class CamperTransportService : ICamperTransportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CamperTransportService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CamperTransportResponseDto>> GetCampersByScheduleIdAsync(int transportScheduleId)
        {
            var camperTransports = await GetCamperTransportsWithIncludes()
                .Where(c => c.transportScheduleId == transportScheduleId).ToListAsync();

            return _mapper.Map<IEnumerable<CamperTransportResponseDto>>(camperTransports);
        }

        public async Task<IEnumerable<CamperTransportResponseDto>> GetActiveCamperTransportsByScheduleIdAsync(int transportScheduleId)
        {
            var camperTransports = await GetCamperTransportsWithIncludes()
                .Where(c => c.transportScheduleId == transportScheduleId && 
                           c.status != CamperTransportStatus.Canceled.ToString())
                .ToListAsync();

            return _mapper.Map<IEnumerable<CamperTransportResponseDto>>(camperTransports);
        }

        public async Task<CamperTransportResponseDto> UpdateStatusAsync(int camperTransportId, CamperTransportUpdateDto updateDto)
        {
            var entity = await _unitOfWork.CamperTransports.GetQueryable()
                .Include(ct => ct.camper)
                .Include(ct => ct.stopLocation)
                .FirstOrDefaultAsync(ct => ct.camperTransportId == camperTransportId)
                ?? throw new NotFoundException("Không tìm thấy bản ghi CamperTransport.");


            // logic update
            if (updateDto.IsAbsent.HasValue)
                entity.isAbsent = updateDto.IsAbsent.Value;

            if (!string.IsNullOrEmpty(updateDto.Status))
                entity.status = updateDto.Status;

            if (!string.IsNullOrEmpty(updateDto.Note))
                entity.note = updateDto.Note;

            // logic check-in
            if (updateDto.CheckInTime.HasValue)
                entity.checkInTime = updateDto.CheckInTime.Value;

            else if (!string.IsNullOrEmpty(updateDto.Status) && updateDto.Status == CamperTransportStatus.Onboard.ToString() && entity.checkInTime == null)
                entity.checkInTime = DateTime.UtcNow;

            // logic check-out
            if (updateDto.CheckOutTime.HasValue)
                entity.checkOutTime = updateDto.CheckOutTime.Value;
            else if (!string.IsNullOrEmpty(updateDto.Status) && updateDto.Status == CamperTransportStatus.Completed.ToString() && entity.checkOutTime == null)
                entity.checkOutTime = DateTime.UtcNow;

            await _unitOfWork.CamperTransports.UpdateAsync(entity);
            await _unitOfWork.CommitAsync();

            var updatedEntity = await GetCamperTransportsWithIncludes()
                .FirstOrDefaultAsync(ct => ct.camperTransportId == camperTransportId);

            return _mapper.Map<CamperTransportResponseDto>(updatedEntity);
        }

        public async Task<bool> GenerateCamperListForScheduleAsync(int transportScheduleId)
        {
            // validation 
            var (schedule, defaultStop, existingCamperIds, validRegistrations) = await prepareDataForGeneration(transportScheduleId);

            var campersToAdd = new List<CamperTransport>();

            foreach (var reg in validRegistrations)
            {
                foreach (var rc in reg.RegistrationCampers)
                {
                    // only take status = confirmed and requests transport, and not already assigned
                    if ((rc.status == RegistrationCamperStatus.Confirmed.ToString() ||
                         rc.status == RegistrationCamperStatus.Confirmed.ToString()) &&
                         rc.requestTransport == true && !existingCamperIds.Contains(rc.camperId))
                    {
                        // create new CamperTransport record 
                        campersToAdd.Add(new CamperTransport
                        {
                            transportScheduleId = transportScheduleId,
                            camperId = rc.camperId,
                            stopLocationId = defaultStop.locationId,
                            status = CamperTransportStatus.Assigned.ToString(),
                            isAbsent = false,
                            checkInTime = null,
                            checkOutTime = null
                        });
                    }
                }
            }

            if (campersToAdd.Any())
            {
                await _unitOfWork.CamperTransports.AddRangeAsync(campersToAdd);
                await _unitOfWork.CommitAsync();
                return true;
            }

            return false;
        }

        public async Task<bool> CamperCheckInAsync(CamperTransportAttendanceDto request)
        {
            // validation and data fetching (BR-GEN-01, BR-CT-05 pre-check)
            var entities = await validateAttendanceRequest(request, CamperTransportStatus.Assigned.ToString());

            foreach (var entity in entities)
            {
                // br-ct-05: check status transition and set time
                // Using TimezoneHelper.GetVietnamNow() inside checkInTransitionLogic
                checkInTransitionLogic(entity, request.Note);

                // update RegistrationCamper status = transporting
                var regCamper = await _unitOfWork.RegistrationCampers.GetByCamperId(entity.camperId);
                if (regCamper != null && regCamper.status != RegistrationCamperStatus.Transporting.ToString())
                {
                    regCamper.status = RegistrationCamperStatus.Transporting.ToString(); 
                    await _unitOfWork.RegistrationCampers.UpdateAsync(regCamper);
                }

                await _unitOfWork.CamperTransports.UpdateAsync(entity);
            }

            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task<bool> CamperCheckOutAsync(CamperTransportAttendanceDto request)
        {
            // validation
            var entities = await validateAttendanceRequest(request, CamperTransportStatus.Onboard.ToString()); 

            var now = TimezoneHelper.GetVietnamNow();

            foreach (var entity in entities)
            {
                // check status, time integrity, and set time
                checkOutTransitionLogic(entity, now, request.Note);

                // update RegistrationCamper status = transported
                if (entity.transportSchedule.transportType == TransportScheduleType.PickUp.ToString())
                {
                    var regCamper = await _unitOfWork.RegistrationCampers.GetByCamperId(entity.camperId);
                    if (regCamper != null && regCamper.status != RegistrationCamperStatus.Transported.ToString())
                    {
                        regCamper.status = RegistrationCamperStatus.Transported.ToString(); 
                        await _unitOfWork.RegistrationCampers.UpdateAsync(regCamper);
                    }
                }

                await _unitOfWork.CamperTransports.UpdateAsync(entity);
            }

            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task<bool> CamperMarkAbsentAsync(CamperTransportAttendanceDto request)
        {
            // validation 
            var entities = await validateAttendanceRequest(request, CamperTransportStatus.Assigned.ToString(), true);

            foreach (var entity in entities)
            {
                // schedule status check for absent logic
                checkAbsentScheduleStatus(entity.transportSchedule);

                markAbsentLogic(entity, request.Note);

                await _unitOfWork.CamperTransports.UpdateAsync(entity);
            }

            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task<IEnumerable<CamperTransportResponseDto>> GetAllCamperTransportAsync()
        {
            var camperTransports = await GetCamperTransportsWithIncludes().ToListAsync();

            return _mapper.Map<IEnumerable<CamperTransportResponseDto>>(camperTransports);

        }

        #region Private Methods

        private IQueryable<CamperTransport> GetCamperTransportsWithIncludes()
        {
            // load related entities
            return _unitOfWork.CamperTransports.GetQueryable()
                .Include(c => c.camper)
                .Include(c => c.stopLocation);
        }


        // get all necessary data for GenerateCamperListForScheduleAsync
        private async Task<(TransportSchedule schedule, RouteStop defaultStop, List<int> existingCamperIds, List<Registration> validRegistrations)> prepareDataForGeneration(int transportScheduleId)
        {
            // check schedule and route existence
            var schedule = await _unitOfWork.TransportSchedules.GetQueryable()
                .Include(s => s.route).ThenInclude(r => r.RouteStops)
                .Include(s => s.route.camp)
                .FirstOrDefaultAsync(s => s.transportScheduleId == transportScheduleId);

            if (schedule == null) throw new NotFoundException("Không tìm thấy Lịch trình xe.");
            if (schedule.route == null) throw new BusinessRuleException("Lịch trình chưa được gán Tuyến đường.");
            if (schedule.route.camp == null) throw new BusinessRuleException("Tuyến đường chưa được gán cho Trại.");

            // check camp status 
            checkCampStatusForGeneration(schedule.route.camp.status);

            // check route stop existence
            var defaultStop = schedule.route.RouteStops.OrderBy(rs => rs.stopOrder).FirstOrDefault();
            if (defaultStop == null) throw new BusinessRuleException("Tuyến đường chưa có điểm dừng (RouteStop) nào.");

            // check valid registrations
            var validRegistrations = await _unitOfWork.Registrations.GetQueryable()
                .Where(r => r.campId == schedule.route.campId &&
                             r.status == RegistrationStatus.Confirmed.ToString())
                .Include(r => r.RegistrationCampers)
                .ToListAsync();

            if (!validRegistrations.Any())
            {
                // returning an empty list of registrations is acceptable, not an error
                throw new BusinessRuleException("Không tìm thấy học viên nào đã xác nhận đăng ký và yêu cầu vận chuyển cho trại này.");
            }

            // get existing camper IDs for idempotency
            var existingCamperIds = await _unitOfWork.CamperTransports.GetQueryable()
                .Where(ct => ct.transportScheduleId == transportScheduleId)
                .Select(ct => ct.camperId)
                .ToListAsync();

            return (schedule, defaultStop, existingCamperIds, validRegistrations);
        }

        private void checkCampStatusForGeneration(string campStatus)
        {
            // only allow generation when camp status = RegistrationClosed or InProgress
            if (campStatus != CampStatus.RegistrationClosed.ToString() &&
                campStatus != CampStatus.InProgress.ToString())
            {
                throw new BusinessRuleException($"Không thể tạo danh sách đưa đón. Trại hiện đang ở trạng thái '{campStatus}'. Yêu cầu trạng thái 'RegistrationClosed' hoặc 'InProgress'.");
            }
        }

        private async Task<List<CamperTransport>> validateAttendanceRequest(CamperTransportAttendanceDto request, string requiredStatus, bool isAbsentCheck = false)
        {
            // check if id list is empty
            if (request.CamperTransportIds == null || !request.CamperTransportIds.Any())
                throw new BadRequestException("Danh sách CamperTransport không được để trống.");

            var entities = await _unitOfWork.CamperTransports.GetQueryable()
                .Include(ct => ct.transportSchedule) // check in absent logic
                .Where(ct => request.CamperTransportIds.Contains(ct.camperTransportId))
                .ToListAsync();

            // check existence
            if (entities.Count != request.CamperTransportIds.Count)
                throw new NotFoundException("Một số CamperTransportId(s) không tìm thấy dữ liệu.");

            foreach (var entity in entities)
            {
                // check status transition for check-in/out
                if (!isAbsentCheck && entity.status != requiredStatus)
                {
                    throw new BusinessRuleException($"Lỗi tại Camper ID {entity.camperId}: Không thể thực hiện. Yêu cầu trạng thái '{requiredStatus}', nhưng hiện tại là '{entity.status}'.");
                }
            }

            return entities;
        }

        private void checkInTransitionLogic(CamperTransport entity, string? note) 
        {
            // check status transition and set time
            var checkInTimeVn = TimezoneHelper.GetVietnamNow();

            entity.status = CamperTransportStatus.Onboard.ToString();
            entity.checkInTime = checkInTimeVn;
            entity.isAbsent = false;
            if (!string.IsNullOrEmpty(note)) entity.note = note;
        }

        private void checkOutTransitionLogic(CamperTransport entity, DateTime now, string? note)
        {

            var checkOutTimeVn = TimezoneHelper.GetVietnamNow();

            // checkOutTime must be after checkInTime
            if (!entity.checkInTime.HasValue || entity.checkInTime.Value >= checkOutTimeVn)
                throw new BusinessRuleException($"Lỗi tại Camper ID {entity.camperId}: Giờ Check-in ({entity.checkInTime}) không hợp lệ hoặc xảy ra sau giờ Check-out.");

            // set time and update status
            entity.status = CamperTransportStatus.Completed.ToString();
            entity.checkOutTime = checkOutTimeVn; 
            if (!string.IsNullOrEmpty(note)) entity.note = note;
        }

        private void checkAbsentScheduleStatus(TransportSchedule schedule)
        {
            // cannot mark absent if schedule is InProgress or Completed
            if (schedule.status == TransportScheduleStatus.InProgress.ToString() ||
                schedule.status == TransportScheduleStatus.Completed.ToString())
            {
                throw new BusinessRuleException($"Không thể đánh dấu vắng mặt. Lịch trình đã bắt đầu hoặc hoàn thành.");
            }
        }

        private void markAbsentLogic(CamperTransport entity, string? note)
        {
            entity.status = CamperTransportStatus.Absent.ToString();
            entity.isAbsent = true;
            entity.checkInTime = null;
            entity.checkOutTime = null;
            if (!string.IsNullOrEmpty(note)) entity.note = note;
        }

        #endregion
    }
}