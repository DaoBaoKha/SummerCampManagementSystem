using SummerCampManagementSystem.BLL.DTOs.AttendanceLog;
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

    }
}
