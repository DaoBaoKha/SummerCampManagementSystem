using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.AttendanceLog;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Services
{
    public class AttendanceLogService : IAttendanceLogService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AttendanceLogService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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

        public async Task<AttendanceLogResponseDto> CheckinAttendanceAsync(AttendanceLogRequestDto attendanceLogDto)
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

            var registrationCamper = await _unitOfWork.RegistrationCampers
                .GetByCamperId(attendanceLogDto.CamperId);

            if (registrationCamper != null)
            {
                registrationCamper.status = "Checkin"; // cập nhật trạng thái
                await _unitOfWork.RegistrationCampers.UpdateAsync(registrationCamper);
            }

            await _unitOfWork.CommitAsync();
            return _mapper.Map<AttendanceLogResponseDto>(attendanceLog);
        }
    }
}
