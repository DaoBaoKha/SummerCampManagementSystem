using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.CamperTransport;
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

        public async Task<CamperTransportResponseDto> UpdateStatusAsync(int camperTransportId, CamperTransportUpdateDto updateDto)
        {
            var entity = await _unitOfWork.CamperTransports.GetQueryable()
                .Include(ct => ct.camper)
                .Include(ct => ct.stopLocation)
                .FirstOrDefaultAsync(ct => ct.camperTransportId == camperTransportId)
                ?? throw new KeyNotFoundException("CamperTransport record not found.");

            if (updateDto.IsAbsent.HasValue)
                entity.isAbsent = updateDto.IsAbsent.Value;

            if (!string.IsNullOrEmpty(updateDto.Status))
                entity.status = updateDto.Status;

            if (!string.IsNullOrEmpty(updateDto.Note))
                entity.note = updateDto.Note;

            // auto set time if null
            if (updateDto.CheckInTime.HasValue)
                entity.checkInTime = updateDto.CheckInTime.Value;
            else if (updateDto.Status == "OnBoard" && entity.checkInTime == null)
                entity.checkInTime = DateTime.UtcNow; 

            if (updateDto.CheckOutTime.HasValue)
                entity.checkOutTime = updateDto.CheckOutTime.Value;
            else if (updateDto.Status == "Completed" && entity.checkOutTime == null)
                entity.checkOutTime = DateTime.UtcNow; 

            await _unitOfWork.CamperTransports.UpdateAsync(entity);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<CamperTransportResponseDto>(entity);
        }

        public async Task<bool> GenerateCamperListForScheduleAsync(int transportScheduleId)
        {
            // take schedule and route
            var schedule = await _unitOfWork.TransportSchedules.GetQueryable()
                .Include(s => s.route).ThenInclude(r => r.RouteStops)
                .Include(s => s.route.camp) // include camp to check status
                .FirstOrDefaultAsync(s => s.transportScheduleId == transportScheduleId);

            if (schedule == null) throw new KeyNotFoundException("Không tìm thấy Lịch trình xe.");
            if (schedule.route == null) throw new InvalidOperationException("Lịch trình chưa được gán Tuyến đường.");
            if (schedule.route.camp == null) throw new InvalidOperationException("Tuyến đường chưa được gán cho Trại.");

            // camp validation
            var campStatus = schedule.route.camp.status;
            if (campStatus != CampStatus.RegistrationClosed.ToString() &&
                campStatus != CampStatus.InProgress.ToString())
            {
                throw new InvalidOperationException($"Không thể tạo danh sách đưa đón. Trại hiện đang ở trạng thái '{campStatus}'. Yêu cầu trạng thái 'RegistrationClosed' hoặc 'InProgress'.");
            }

            // registration validation with same campId and status = confirmed
            var validRegistrations = await _unitOfWork.Registrations.GetQueryable()
                .Where(r => r.campId == schedule.route.campId &&
                            r.status == RegistrationStatus.Confirmed.ToString())
                .Include(r => r.RegistrationCampers)
                .ToListAsync();

            if (!validRegistrations.Any())
            {
                return false;
            }

            // get pickup or dropoff point from route
            var defaultStop = schedule.route.RouteStops.OrderBy(rs => rs.stopOrder).FirstOrDefault();
            if (defaultStop == null) throw new InvalidOperationException("Tuyến đường chưa có điểm dừng (RouteStop) nào.");

            var campersToAdd = new List<CamperTransport>();

            // get list id camper in the schedule to avoid duplicate (idempotency)
            var existingCamperIds = await _unitOfWork.CamperTransports.GetQueryable()
                .Where(ct => ct.transportScheduleId == transportScheduleId)
                .Select(ct => ct.camperId)
                .ToListAsync();

            // validate camper
            foreach (var reg in validRegistrations)
            {
                foreach (var rc in reg.RegistrationCampers)
                {
                    // only take status = confirmed
                    if ((rc.status == RegistrationCamperStatus.Confirmed.ToString() ||
                         rc.status == RegistrationCamperStatus.Confirmed.ToString()) &&
                         rc.requestTransport == true && !existingCamperIds.Contains(rc.camperId))
                    {
                        // check duplicate
                        if (!existingCamperIds.Contains(rc.camperId))
                        {
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
            }

            if (campersToAdd.Any())
            {
                await _unitOfWork.CamperTransports.AddRangeAsync(campersToAdd);
                await _unitOfWork.CommitAsync();
            }

            return true;
        }

        public async Task<bool> CamperCheckInAsync(CamperTransportAttendanceDto request)
        {
            var entity = await _unitOfWork.CamperTransports.GetByIdAsync(request.CamperTransportId)
                ?? throw new KeyNotFoundException("Không tìm thấy dữ liệu.");

            await ValidateTripInProgressAsync(entity.transportScheduleId);  

            if (entity.status != CamperTransportStatus.Assigned.ToString())
                throw new InvalidOperationException($"Không thể Check-in. Trạng thái hiện tại: {entity.status}");

            entity.status = CamperTransportStatus.Onboard.ToString();
            entity.checkInTime = TimezoneHelper.GetVietnamNow();
            entity.isAbsent = false;

            if (!string.IsNullOrEmpty(request.Note)) entity.note = request.Note;

            await _unitOfWork.CamperTransports.UpdateAsync(entity);
            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task<bool> CamperCheckOutAsync(CamperTransportAttendanceDto request)
        {
            var entity = await _unitOfWork.CamperTransports.GetByIdAsync(request.CamperTransportId)
                ?? throw new KeyNotFoundException("Không tìm thấy dữ liệu.");

            // check status = Onboard before checkout
            if (entity.status != CamperTransportStatus.Onboard.ToString())
                throw new InvalidOperationException($"Không thể Check-out. Camper chưa lên xe (Status: {entity.status})");

            entity.status = CamperTransportStatus.Completed.ToString();
            entity.checkOutTime = TimezoneHelper.GetVietnamNow();

            if (!string.IsNullOrEmpty(request.Note)) entity.note = request.Note;

            await _unitOfWork.CamperTransports.UpdateAsync(entity);
            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task<bool> CamperMarkAbsentAsync(CamperTransportAttendanceDto request)
        {
            var entity = await _unitOfWork.CamperTransports.GetByIdAsync(request.CamperTransportId)
                ?? throw new KeyNotFoundException("Không tìm thấy dữ liệu.");

            await ValidateTripInProgressAsync(entity.transportScheduleId);

            entity.status = CamperTransportStatus.Absent.ToString();
            entity.isAbsent = true;
            entity.checkInTime = null;
            entity.checkOutTime = null;

            if (!string.IsNullOrEmpty(request.Note)) entity.note = request.Note;

            await _unitOfWork.CamperTransports.UpdateAsync(entity);
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
            //load related entities
            return _unitOfWork.CamperTransports.GetQueryable()
                .Include(c => c.camper)
                .Include(c => c.stopLocation);
        }

        private async Task ValidateTripInProgressAsync(int transportScheduleId)
        {
            var schedule = await _unitOfWork.TransportSchedules.GetByIdAsync(transportScheduleId);

            if (schedule == null)
                throw new KeyNotFoundException("Không tìm thấy lịch trình chuyến đi.");

            if (schedule.status != TransportScheduleStatus.InProgress.ToString())
            {
                throw new InvalidOperationException($"Chuyến đi chưa bắt đầu (Trạng thái hiện tại: {schedule.status}). Vui lòng bấm 'Start Trip'!");
            }
        }
        #endregion
    }
}