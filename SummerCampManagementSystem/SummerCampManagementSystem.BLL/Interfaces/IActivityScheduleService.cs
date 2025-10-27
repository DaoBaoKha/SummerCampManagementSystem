using SummerCampManagementSystem.BLL.DTOs.ActivitySchedule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IActivityScheduleService
    {
        Task<ActivityScheduleResponseDto> CreateCoreScheduleAsync(ActivityScheduleCreateDto dto);
        Task<ActivityScheduleResponseDto> CreateOptionalScheduleAsync(OptionalScheduleCreateDto dto, int coreScheduleId);
        Task<IEnumerable<ActivityScheduleResponseDto>> GetAllSchedulesAsync();
        Task<IEnumerable<ActivityScheduleResponseDto>> GetByCampAndStaffAsync(int campId, int staffId);
        Task<IEnumerable<ActivityScheduleByCamperResponseDto>> GetSchedulesByCamperAndCampAsync(int camperId, int campId);
        Task<IEnumerable<ActivityScheduleResponseDto>> GetOptionalSchedulesByCampAsync(int campId);
        Task<IEnumerable<ActivityScheduleResponseDto>> GetCoreSchedulesByCampAsync(int campId);
        Task<IEnumerable<ActivityScheduleResponseDto>> GetSchedulesByCampAsync(int campId);
        Task<IEnumerable<ActivityScheduleResponseDto>> GetSchedulesByDateAsync(DateTime fromDate, DateTime toDate);
        Task<ActivityScheduleResponseDto> UpdateCoreScheduleAsync(int id, ActivityScheduleCreateDto dto);
    }
}
