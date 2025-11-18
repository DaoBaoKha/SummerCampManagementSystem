using SummerCampManagementSystem.BLL.DTOs.AttendanceLog;
using SummerCampManagementSystem.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IAttendanceLogService
    {
        Task<IEnumerable<AttendanceLogResponseDto>> GetAllAttendanceLogsAsync();
        Task<AttendanceLogResponseDto?> GetAttendanceLogByIdAsync(int id);
        Task<AttendanceLogResponseDto> CoreActivityAttendanceAsync(AttendanceLogRequestDto attendanceLogDto);
        Task<AttendanceLogResponseDto> OptionalActivityAttendanceAsync(AttendanceLogRequestDto attendanceLogDto);
        Task<object> Checkin_CheckoutAttendanceAsync(AttendanceLogListRequestDto attendanceLogDto, int StaffId, RegistrationCamperStatus status);
        Task<AttendanceLogResponseDto> RestingAttendanceAsync(AttendanceLogRequestDto attendanceLogDto);
        Task<object> CoreActivityAttendanceAsync(AttendanceLogListRequestDto dto, int staffId, bool commit = true);
        Task CreateAttendanceLogsForClosedCampsAsync();
        Task UpdateAttendanceLogAsync(List<AttendanceLogUpdateRequest> updates, int staffId);

    }
}
