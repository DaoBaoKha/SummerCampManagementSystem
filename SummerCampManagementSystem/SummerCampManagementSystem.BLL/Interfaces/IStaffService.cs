using Microsoft.AspNetCore.Http;
using SummerCampManagementSystem.BLL.DTOs.UserAccount;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IStaffService
    {
        Task<IEnumerable<StaffSummaryDto>> GetAvailableActivityStaffs(int campId, int activityScheduleId);
        Task<IEnumerable<StaffSummaryDto>> GetAvailableActivityStaffsByTime(int campId, DateTime startTime, DateTime endTime);
        Task<IEnumerable<StaffSummaryDto>> GetAvailableGroupStaffs(int campId);
        Task<IEnumerable<StaffSummaryDto>> GetAvailableAccomodationStaffs(int campId);
        Task<string> UpdateStaffAvatarAsync(int userId, IFormFile file);
    }
}
