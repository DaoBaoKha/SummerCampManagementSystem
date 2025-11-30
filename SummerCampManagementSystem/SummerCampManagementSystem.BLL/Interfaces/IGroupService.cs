using SummerCampManagementSystem.BLL.DTOs.Group;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IGroupService
    {
        Task<IEnumerable<GroupResponseDto>> GetAllGroupsAsync();
        Task<GroupResponseDto?> GetGroupByIdAsync(int id);
        Task<GroupResponseDto> CreateGroupAsync(GroupRequestDto Group);
        Task<GroupResponseDto?> UpdateGroupAsync(int id, GroupRequestDto Group);
        Task<bool> DeleteGroupAsync(int id);
        Task<GroupResponseDto> AssignStaffToGroup(int GroupId, int staffId);
        Task<GroupWithCampDetailsResponseDto?> GetGroupBySupervisorIdAsync(int supervisorId, int campId);
        Task<IEnumerable<GroupResponseDto>> GetGroupsByActivityScheduleId(int activityScheduleId);
        Task<IEnumerable<GroupResponseDto>> GetGroupsByCampIdAsync(int campId);
    }
}
