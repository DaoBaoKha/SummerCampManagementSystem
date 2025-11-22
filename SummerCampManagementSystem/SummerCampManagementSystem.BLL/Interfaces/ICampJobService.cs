using SummerCampManagementSystem.BLL.DTOs.CampJob;
using SummerCampManagementSystem.Core.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface ICampJobService
    {
        Task ScheduleJobsForCampAsync(int campId);
        Task DeleteAllJobsForCampAsync(int campId);
        Task<CampJobListDto> GetJobsForCampAsync(int campId);
        Task<JobExecutionResultDto> ForceRunJobAsync(string jobName);
        Task RebuildJobsForCampAsync(int campId);
        Task<List<CampJobInfoDto>> GetAllJobsAsync();
        Task ExecuteStatusTransitionJobAsync(int campId, CampStatus targetStatus, string jobName);
    }
}
