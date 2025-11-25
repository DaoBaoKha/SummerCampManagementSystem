using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.AttendanceLog;
using SummerCampManagementSystem.BLL.DTOs.Camper;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace SummerCampManagementSystem.BLL.Services
{
    public class AttendanceLogService : IAttendanceLogService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICamperService _camperService;

        public AttendanceLogService(IUnitOfWork unitOfWork, IMapper mapper, ICamperService camperService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _camperService = camperService;
        }

        public async Task<IEnumerable<AttendanceLogResponseDto>> GetAllAttendanceLogsAsync()
        {
            var attendanceLogs = await _unitOfWork.AttendanceLogs.GetAllAsync();
            return _mapper.Map<IEnumerable<AttendanceLogResponseDto>>(attendanceLogs);
        }

        public async Task<AttendanceLogResponseDto?> GetAttendanceLogByIdAsync(int id)
        {
            var attendanceLog = await _unitOfWork.AttendanceLogs.GetByIdAsync(id);
            return attendanceLog == null ? null : _mapper.Map<AttendanceLogResponseDto>(attendanceLog);
        }

        public async Task<object> CoreActivityAttendanceAsync(AttendanceLogListRequestDto dto, int staffId, bool commit = true)
        {
            var activitySchedule = await _unitOfWork.ActivitySchedules.GetByIdAsync(dto.ActivityScheduleId)
                ?? throw new KeyNotFoundException("Activity Schedule not found.");

            var activity = await _unitOfWork.Activities.GetByIdAsync(activitySchedule.activityId)
                ?? throw new KeyNotFoundException("The Activity Schedule does not have any activities.");

            if (activitySchedule.coreActivityId != null)
                throw new InvalidOperationException("This is not a core Activity Schedule.");

            int success = 0, fail = 0;
            List<int> failedCamperIds = new();

            foreach (var camperId in dto.CamperIds)
            {
                var camper = await _unitOfWork.Campers.GetByIdAsync(camperId);
                if (camper == null)
                {
                    failedCamperIds.Add(camperId);
                    fail++;
                    continue;
                }

                // Tạo record attendance
                var attendance = new AttendanceLog
                {
                    camperId = camperId,
                    staffId = staffId,
                    activityScheduleId = dto.ActivityScheduleId,
                    participantStatus = dto.participantStatus.ToString(),
                    timestamp = DateTime.UtcNow,
                    checkInMethod = "Manual",
                    eventType = "string"
                };

                await _unitOfWork.AttendanceLogs.CreateAsync(attendance);
                success++;
            }

            if(activitySchedule != null)
            {
                activitySchedule.status = "AttendanceChecked";
                await _unitOfWork.ActivitySchedules.UpdateAsync(activitySchedule);
            }

            if (commit)
                await _unitOfWork.CommitAsync();

            return new
            {
                total = dto.CamperIds.Count,
                success,
                fail,
                failedCamperIds
            };
        }

        public async Task<object> Checkin_CheckoutAttendanceAsync(AttendanceLogListRequestDto attendanceLogDto, int StaffId, RegistrationCamperStatus status)
        {
            var attendanceLog = await CoreActivityAttendanceAsync(attendanceLogDto, StaffId, false);

            var updateTasks = attendanceLogDto.CamperIds.Select(async camperId =>
            {
                var registrationCamper = await _unitOfWork.RegistrationCampers.GetByCamperId(camperId);
                if (registrationCamper != null)
                {
                    registrationCamper.status = status.ToString();
                    await _unitOfWork.RegistrationCampers.UpdateAsync(registrationCamper);
                }
            });
            await Task.WhenAll(updateTasks);

            await _unitOfWork.CommitAsync();
            return attendanceLog;
        }

        public async Task<AttendanceLogResponseDto> CoreActivityAttendanceAsync(AttendanceLogRequestDto attendanceLogDto)
        {
            var camper = await _unitOfWork.Campers.GetByIdAsync(attendanceLogDto.CamperId)
                ?? throw new KeyNotFoundException("Camper not found.");
            var staff = await _unitOfWork.Users.GetByIdAsync(attendanceLogDto.StaffId)
                ?? throw new KeyNotFoundException("Staff not found.");

            if (!string.Equals(staff.role, "Staff", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Assigned user is not a staff member.");
            }

            bool isGroupStaff = await _unitOfWork.Campers.IsStaffSupervisorOfCamperAsync(attendanceLogDto.StaffId, attendanceLogDto.CamperId);

            if (!isGroupStaff)
            {
                throw new InvalidOperationException("Staff is not a supervisor of the camper.");
            }

            var activitySchedule = await _unitOfWork.ActivitySchedules.GetByIdAsync(attendanceLogDto.ActivityScheduleId)
                ?? throw new KeyNotFoundException("Activity Schedule not found.");

            var activity = await _unitOfWork.Activities.GetByIdAsync(activitySchedule.activityId)
                ?? throw new KeyNotFoundException("The Activity Schedule does not have any activities.");

            if (activitySchedule.coreActivityId != null)
            {
                throw new InvalidOperationException("This is not a core Activity Schedule.");
            }

            bool isCamperInActivity = await _unitOfWork.AttendanceLogs.IsCoreScheduleOfCamper(camper.groupId.Value, attendanceLogDto.ActivityScheduleId);
            if (!isCamperInActivity)
            {
                throw new InvalidOperationException("This Camper does not participate in this Activity Schedule.");
            }

            var attendanceLog = _mapper.Map<AttendanceLog>(attendanceLogDto);

            await _unitOfWork.AttendanceLogs.CreateAsync(attendanceLog);


            await _unitOfWork.CommitAsync();
            return _mapper.Map<AttendanceLogResponseDto>(attendanceLog);
        }

        public async Task<AttendanceLogResponseDto> OptionalActivityAttendanceAsync(AttendanceLogRequestDto attendanceLogDto)
        {
            var camper = await _unitOfWork.Campers.GetByIdAsync(attendanceLogDto.CamperId)
                ?? throw new KeyNotFoundException("Camper not found.");

            var activitySchedule = await _unitOfWork.ActivitySchedules.GetByIdAsync(attendanceLogDto.ActivityScheduleId)
                ?? throw new KeyNotFoundException("Activity Schedule not found.");

            var activity = await _unitOfWork.Activities.GetByIdAsync(activitySchedule.activityId)
                ?? throw new KeyNotFoundException("The Activity Schedule does not have any activities.");

            if (activitySchedule.coreActivityId == null)
            {
                throw new InvalidOperationException("This is not an optional Activity Schedule.");
            }

            bool isCamperInActivity = await _unitOfWork.AttendanceLogs.IsOptionalScheduleOfCamper(camper.camperId, attendanceLogDto.ActivityScheduleId);
            
            if (!isCamperInActivity)
            {
                throw new InvalidOperationException("This Camper does not participate in this Activity Schedule.");
            }

            var staff = await _unitOfWork.Users.GetByIdAsync(attendanceLogDto.StaffId)
               ?? throw new KeyNotFoundException("Staff not found.");

            if (!string.Equals(staff.role, "Staff", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Assigned user is not a staff member.");
            }

            bool isActivityStaff = await _unitOfWork.ActivitySchedules.IsStaffOfActivitySchedule(attendanceLogDto.StaffId, attendanceLogDto.ActivityScheduleId);

            if (!isActivityStaff)
            {
                throw new InvalidOperationException("Staff is not assigned to the activity schedule.");
            }

            var attendanceLog = _mapper.Map<AttendanceLog>(attendanceLogDto);
            await _unitOfWork.AttendanceLogs.CreateAsync(attendanceLog);
            await _unitOfWork.CommitAsync();
            return _mapper.Map<AttendanceLogResponseDto>(attendanceLog);

        }

      

        //public async Task<AttendanceLogResponseDto> CheckinAttendanceAsync(AttendanceLogRequestDto attendanceLogDto)
        //{
        //    var camper = await _unitOfWork.Campers.GetByIdAsync(attendanceLogDto.CamperId)
        //        ?? throw new KeyNotFoundException("Camper not found.");
        //    var staff = await _unitOfWork.Users.GetByIdAsync(attendanceLogDto.StaffId)
        //        ?? throw new KeyNotFoundException("Staff not found.");

        //    if (!string.Equals(staff.role, "Staff", StringComparison.OrdinalIgnoreCase))
        //    {
        //        throw new InvalidOperationException("Assigned user is not a staff member.");
        //    }

        //    bool isGroupStaff = await _unitOfWork.Campers.IsStaffSupervisorOfCamperAsync(attendanceLogDto.StaffId, attendanceLogDto.CamperId);

        //    if (!isGroupStaff)
        //    {
        //        throw new InvalidOperationException("Staff is not an accomodation supervisor of the camper.");
        //    }

        //    var activitySchedule = await _unitOfWork.ActivitySchedules.GetByIdAsync(attendanceLogDto.ActivityScheduleId)
        //        ?? throw new KeyNotFoundException("Activity Schedule not found.");

        //    var activity = await _unitOfWork.Activities.GetByIdAsync(activitySchedule.activityId)
        //        ?? throw new KeyNotFoundException("The Activity Schedule does not have any activities.");

        //    if (activitySchedule.coreActivityId != null)
        //    {
        //        throw new InvalidOperationException("This is not a core Activity Schedule.");
        //    }

        //    bool isCamperInActivity = await _unitOfWork.AttendanceLogs.IsCoreScheduleOfCamper(camper.groupId.Value, attendanceLogDto.ActivityScheduleId);
        //    if (!isCamperInActivity)
        //    {
        //        throw new InvalidOperationException("This Camper does not participate in this Activity Schedule.");
        //    }


        //    var attendanceLog = _mapper.Map<AttendanceLog>(attendanceLogDto);
        //    await _unitOfWork.AttendanceLogs.CreateAsync(attendanceLog);

        //    var registrationCamper = await _unitOfWork.RegistrationCampers
        //        .GetByCamperId(attendanceLogDto.CamperId);

        //    if (registrationCamper != null)
        //    {
        //        registrationCamper.status = "Checkin"; // cập nhật trạng thái
        //        await _unitOfWork.RegistrationCampers.UpdateAsync(registrationCamper);
        //    }

        //    await _unitOfWork.CommitAsync();
        //    return _mapper.Map<AttendanceLogResponseDto>(attendanceLog);
        //}


        //public async Task<AttendanceLogResponseDto> CheckoutAttendanceAsync(AttendanceLogRequestDto attendanceLogDto)
        //{
        //    var camper = await _unitOfWork.Campers.GetByIdAsync(attendanceLogDto.CamperId)
        //        ?? throw new KeyNotFoundException("Camper not found.");
        //    var staff = await _unitOfWork.Users.GetByIdAsync(attendanceLogDto.StaffId)
        //        ?? throw new KeyNotFoundException("Staff not found.");

        //    if (!string.Equals(staff.role, "Staff", StringComparison.OrdinalIgnoreCase))
        //    {
        //        throw new InvalidOperationException("Assigned user is not a staff member.");
        //    }

        //    bool isGroupStaff = await _unitOfWork.Campers.IsStaffSupervisorOfCamperAsync(attendanceLogDto.StaffId, attendanceLogDto.CamperId);

        //    if (!isGroupStaff)
        //    {
        //        throw new InvalidOperationException("Staff is not an accomodation supervisor of the camper.");
        //    }

        //    var activitySchedule = await _unitOfWork.ActivitySchedules.GetByIdAsync(attendanceLogDto.ActivityScheduleId)
        //        ?? throw new KeyNotFoundException("Activity Schedule not found.");

        //    var activity = await _unitOfWork.Activities.GetByIdAsync(activitySchedule.activityId)
        //        ?? throw new KeyNotFoundException("The Activity Schedule does not have any activities.");

        //    if (activitySchedule.coreActivityId != null)
        //    {
        //        throw new InvalidOperationException("This is not a core Activity Schedule.");
        //    }

        //    bool isCamperInActivity = await _unitOfWork.AttendanceLogs.IsCoreScheduleOfCamper(camper.groupId.Value, attendanceLogDto.ActivityScheduleId);
        //    if (!isCamperInActivity)
        //    {
        //        throw new InvalidOperationException("This Camper does not participate in this Activity Schedule.");
        //    }


        //    var attendanceLog = _mapper.Map<AttendanceLog>(attendanceLogDto);
        //    await _unitOfWork.AttendanceLogs.CreateAsync(attendanceLog);

        //    var registrationCamper = await _unitOfWork.RegistrationCampers
        //        .GetByCamperId(attendanceLogDto.CamperId);

        //    if (registrationCamper != null)
        //    {
        //        registrationCamper.status = "Checkout";
        //        await _unitOfWork.RegistrationCampers.UpdateAsync(registrationCamper);
        //    }

        //    await _unitOfWork.CommitAsync();
        //    return _mapper.Map<AttendanceLogResponseDto>(attendanceLog);
        //}

        public async Task<AttendanceLogResponseDto> RestingAttendanceAsync(AttendanceLogRequestDto attendanceLogDto)
        {
            var camper = await _unitOfWork.Campers.GetByIdAsync(attendanceLogDto.CamperId)
                ?? throw new KeyNotFoundException("Camper not found.");
            var staff = await _unitOfWork.Users.GetByIdAsync(attendanceLogDto.StaffId)
                ?? throw new KeyNotFoundException("Staff not found.");

            if (!string.Equals(staff.role, "Staff", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Assigned user is not a staff member.");
            }

            bool isGroupStaff = await _unitOfWork.CamperAccommodations.IsAccommodationStaffOfCamper(attendanceLogDto.StaffId, attendanceLogDto.CamperId);

            if (!isGroupStaff)
            {
                throw new InvalidOperationException("Staff is not an accomodation supervisor of the camper.");
            }

            var activitySchedule = await _unitOfWork.ActivitySchedules.GetByIdAsync(attendanceLogDto.ActivityScheduleId)
                ?? throw new KeyNotFoundException("Activity Schedule not found.");

            var activity = await _unitOfWork.Activities.GetByIdAsync(activitySchedule.activityId)
                ?? throw new KeyNotFoundException("The Activity Schedule does not have any activities.");

            if (activitySchedule.coreActivityId != null)
            {
                throw new InvalidOperationException("This is not a core Activity Schedule.");
            }

            bool isCamperInActivity = await _unitOfWork.AttendanceLogs.IsCoreScheduleOfCamper(camper.groupId.Value, attendanceLogDto.ActivityScheduleId);
            if (!isCamperInActivity)
            {
                throw new InvalidOperationException("This Camper does not participate in this Activity Schedule.");
            }

            var attendanceLog = _mapper.Map<AttendanceLog>(attendanceLogDto);
            await _unitOfWork.AttendanceLogs.CreateAsync(attendanceLog);

            await _unitOfWork.CommitAsync();
            return _mapper.Map<AttendanceLogResponseDto>(attendanceLog);
        }

        public async Task UpdateAttendanceLogAsync(List<AttendanceLogUpdateRequest> updates, int staffId)
        {
            if (updates == null || updates.Count == 0)
                return;

            var logIds = updates.Select(u => u.AttendanceLogId).ToList();

            // Lấy tất cả log 1 lần
            var logs = await _unitOfWork.AttendanceLogs.GetQueryable()
                .Where(l => logIds.Contains(l.attendanceLogId))
                .ToListAsync();

            foreach (var log in logs)
            {
                var req = updates.First(u => u.AttendanceLogId == log.attendanceLogId);
                log.participantStatus = req.participantStatus.ToString();
                log.timestamp = DateTime.UtcNow;
                log.staffId = staffId;
                log.note = req.Note;
                await _unitOfWork.AttendanceLogs.UpdateAsync(log);

            }

            await _unitOfWork.CommitAsync();
        }


        public async Task CreateAttendanceLogsForClosedCampsAsync()
        {
            // 1. Lấy tất cả camp đang RegistrationClosed
            var closedCamps = await _unitOfWork.Camps.GetQueryable()
                .Where(c => c.status == CampStatus.RegistrationClosed.ToString())
                .ToListAsync();

            foreach (var camp in closedCamps)
            {
                // 2. Lấy tất cả activity schedules của camp
                var activities = await _unitOfWork.ActivitySchedules.GetScheduleByCampIdAsync(camp.campId);

                foreach (var activity in activities)
                {
                    IEnumerable<CamperSummaryDto> campers;

                    if (activity.activity.activityType == ActivityType.Core.ToString())
                    {
                        // Core activity: lấy tất cả camper trong group (API đã xử lý logic optional)
                        campers = await _camperService.GetCampersByCoreActivityIdAsync(activity.activityScheduleId);
                    }
                    else // Optional
                    {
                        campers = await _camperService.GetCampersByOptionalActivitySchedule(activity.activityScheduleId);
                    }

                    foreach (var camper in campers)
                    {
                        // Idempotent: kiểm tra log đã tồn tại chưa
                        bool exists = await _unitOfWork.AttendanceLogs.GetQueryable()
                            .AnyAsync(l =>
                                l.camperId == camper.CamperId &&
                                l.activityScheduleId == activity.activityScheduleId);

                        if (!exists)
                        {
                            var log = new AttendanceLog
                            {
                                camperId = camper.CamperId,
                                activityScheduleId = activity.activityScheduleId,
                                participantStatus = ParticipationStatus.NotYet.ToString(),
                                eventType = "string",
                                checkInMethod = "SystemGenerated",
                                timestamp = DateTime.UtcNow
                            };
                            await _unitOfWork.AttendanceLogs.CreateAsync(log);
                        }
                    }
                }
            }

            // Lưu tất cả log 1 lần
            await _unitOfWork.CommitAsync();
        }
    }
}
