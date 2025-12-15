using SummerCampManagementSystem.BLL.DTOs.Activity;
using SummerCampManagementSystem.BLL.DTOs.ActivitySchedule;
using SummerCampManagementSystem.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IActivityScheduleService
    {
        //Task<ActivityScheduleResponseDto> CreateCoreScheduleAsync(ActivityScheduleCreateDto dto);
        Task<CreateScheduleBatchResult> CreateCoreScheduleAsync(ActivityScheduleCreateDto dto);
        Task<CreateScheduleBatchResult> CreateOptionalScheduleAsync(OptionalScheduleCreateDto dto);
        Task<IEnumerable<ActivityScheduleResponseDto>> GetAllSchedulesAsync();
        Task<ActivityScheduleResponseDto?> GetScheduleByIdAsync(int id);
        Task<IEnumerable<ActivityScheduleResponseDto>> GetByCampAndStaffAsync(int campId, int staffId);
        Task<IEnumerable<ActivityScheduleResponseDto>> GetCheckInCheckoutByCampAndStaffAsync(int campId, int staffId);
        Task<IEnumerable<ActivityScheduleByCamperResponseDto>> GetSchedulesByCamperAndCampAsync(int campId, int camperId);
        Task<IEnumerable<ActivityScheduleResponseDto>> GetOptionalSchedulesByCampAsync(int campId);
        Task<IEnumerable<ActivityScheduleResponseDto>> GetCoreSchedulesByCampAsync(int campId);
        Task<IEnumerable<ActivityScheduleResponseDto>> GetSchedulesByCampAsync(int campId);
        Task<IEnumerable<ActivityScheduleResponseDto>> GetSchedulesByDateAsync(DateTime fromDate, DateTime toDate);
        Task<ActivityScheduleResponseDto> UpdateCoreScheduleAsync(int id, ActivityScheduleCreateDto dto);
        Task<object> GetAllSchedulesByStaffIdAsync(int staffId, int campId);
        Task<ActivityScheduleResponseDto> ChangeStatusActivitySchedule(int activityScheduleId, ActivityScheduleStatus status);
        Task<ActivityScheduleResponseDto> UpdateLiveStreamStatus(int activityScheduleId, bool status);
        Task<IEnumerable<ActivityScheduleResponseDto>> GetSchedulesByGroupStaffAsync(int campId, int staffId);
        Task<bool> DeleteActivityScheduleAsync(int activityScheduleId);
        Task ChangeActivityScheduleStatusAuto();
        Task ChangeActityScheduleToPendingAttendance();
        Task<IEnumerable<ActivityScheduleResponseDto>> GenerateCoreSchedulesFromTemplateAsync(ActivityScheduleTemplateDto templateDto);
    }
}
