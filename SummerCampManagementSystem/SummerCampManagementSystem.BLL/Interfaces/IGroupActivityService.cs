using SummerCampManagementSystem.BLL.DTOs.GroupActivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IGroupActivityService
    {
       Task<GroupActivityResponseDto> CreateGroupActivity(GroupActivityDto groupActivityDto);
       Task<bool> RemoveGroupActivity(int groupId, int activityScheduleId);
    }
}
