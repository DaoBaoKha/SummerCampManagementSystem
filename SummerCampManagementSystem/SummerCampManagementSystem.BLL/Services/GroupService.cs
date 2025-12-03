using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.Group;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class GroupService : IGroupService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidationService _validationService;
        private readonly IMapper _mapper;
        public GroupService(IUnitOfWork unitOfWork, IValidationService validationService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _validationService = validationService;
            _mapper = mapper;
        }

        public async Task<IEnumerable<GroupResponseDto>> GetAllGroupsAsync()
        {
            var groups = await _unitOfWork.Groups.GetAllAsync();
            return _mapper.Map<IEnumerable<GroupResponseDto>>(groups);
        }

        public async Task<GroupWithCampDetailsResponseDto?> GetGroupBySupervisorIdAsync(int supervisorId, int campId)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId);
            if (camp == null)
                throw new KeyNotFoundException($"Camp with ID {campId} not found.");

            var group = await _unitOfWork.Groups.GetGroupBySupervisorIdAsync(supervisorId, campId);

            if (group == null)
            {
                throw new KeyNotFoundException($"Camper Group supervised by Staff ID {supervisorId} in Camp ID {campId} not found.");
            }

            return _mapper.Map<GroupWithCampDetailsResponseDto>(group);
        }

        public async Task<GroupResponseDto?> GetGroupByIdAsync(int id)
        {
            var Group = await _unitOfWork.Groups.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Camper Group with ID {id} not found.");

            return Group == null ? null : _mapper.Map<GroupResponseDto>(Group);
        }

        public async Task<GroupResponseDto> CreateGroupAsync(GroupRequestDto Group)
        {
            await RunGroupSupervisorValidation(Group.SupervisorId, Group.CampId);

            var group = _mapper.Map<Group>(Group);


            await _unitOfWork.Groups.CreateAsync(group);
            await _unitOfWork.CommitAsync();

            var coreSchedules = await _unitOfWork.ActivitySchedules.GetCoreScheduleByCampIdAsync(Group.CampId);

            foreach (var core in coreSchedules)
            {
                var groupActivity = new GroupActivity
                {
                    groupId = group.groupId,
                    activityScheduleId = core.activityScheduleId
                };
                await _unitOfWork.GroupActivities.CreateAsync(groupActivity);
            }

            await _unitOfWork.CommitAsync();
            
            return _mapper.Map<GroupResponseDto>(group);
        }

        public async Task<GroupResponseDto> AssignStaffToGroup(int GroupId, int staffId)
        {
            var group = await _unitOfWork.Groups.GetByIdAsync(GroupId)
                ?? throw new KeyNotFoundException($"Camper Group with ID {GroupId} not found.");

            var campId = group.campId ?? throw new InvalidOperationException("Camper Group is not assigned to a Camp.");

            await RunGroupSupervisorValidation(staffId, campId, GroupId);

            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new KeyNotFoundException("Camp not found."); 

            var staff = await _unitOfWork.Users.GetByIdAsync(staffId)
                ?? throw new KeyNotFoundException("Staff not found."); 

            bool staffConflict = await _unitOfWork.ActivitySchedules
            .IsStaffBusyAsync(staffId, camp.startDate.Value, camp.endDate.Value);

            if (staffConflict)
                throw new InvalidOperationException("Staff has another activity scheduled during this time.");

            group.supervisorId = staffId;
            await _unitOfWork.Groups.UpdateAsync(group);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<GroupResponseDto>(group);
        }

        public async Task<GroupResponseDto?> UpdateGroupAsync(int id, GroupRequestDto Group)
        {
            var existingGroup = await _unitOfWork.Groups.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Camper Group with ID {id} not found.");

            await RunGroupSupervisorValidation(Group.SupervisorId, Group.CampId, id);

            _mapper.Map(Group, existingGroup);

            await _unitOfWork.Groups.UpdateAsync(existingGroup);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<GroupResponseDto>(existingGroup);
        }

        public async Task<bool> DeleteGroupAsync(int id)
        {
            var existingGroup = await _unitOfWork.Groups.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Camper Group with ID {id} not found."); 

            await _unitOfWork.Groups.RemoveAsync(existingGroup);
            await _unitOfWork.CommitAsync();

            return true;
        }

        public async Task<IEnumerable<GroupResponseDto>> GetGroupsByActivityScheduleId(int activityScheduleId)
        {
            var activitySchedule = await _unitOfWork.ActivitySchedules.GetByIdAsync(activityScheduleId)
                ?? throw new KeyNotFoundException("Activity Schedule not found.");
            var groups = await _unitOfWork.Groups.GetGroupsByActivityScheduleIdAsync(activityScheduleId);
            return _mapper.Map<IEnumerable<GroupResponseDto>>(groups);
        }


        public async Task<IEnumerable<GroupResponseDto>> GetGroupsByCampIdAsync(int campId)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new KeyNotFoundException($"Camp with ID {campId} not found.");

            var groups = await _unitOfWork.Groups.GetQueryable()
                .Include(g => g.supervisor)
                .Where(g => g.campId == campId)
                .ToListAsync();

            return _mapper.Map<IEnumerable<GroupResponseDto>>(groups);
        }

        #region Private Methods

        private async Task<(UserAccount staff, Camp camp)> RunGroupSupervisorValidation(int? supervisorId, int campId, int? groupId = null)
        {
            // if supervisorId is null or <= 0, skip validation and return camp only
            if (!supervisorId.HasValue || supervisorId.Value <= 0)
            {
                var campOnly = await _unitOfWork.Camps.GetByIdAsync(campId)
                    ?? throw new KeyNotFoundException($"Camp with ID {campId} not found.");
                return (null, campOnly);
            }

            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new KeyNotFoundException($"Camp with ID {campId} not found.");

            var staff = await _unitOfWork.Users.GetByIdAsync(supervisorId.Value)
                ?? throw new KeyNotFoundException($"Supervisor with ID {supervisorId.Value} not found.");

            // check role
            if (!string.Equals(staff.role, "Staff", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"User with ID {supervisorId.Value} is a '{staff.role}', not a Staff member.");
            }

            // check if staff is already assigned to another group in the same camp 
            var existingGroup = await _unitOfWork.Groups.GetGroupBySupervisorIdAsync(supervisorId.Value, campId);

            if (existingGroup != null && existingGroup.groupId != groupId)
            {
                throw new InvalidOperationException($"Supervisor ID {supervisorId.Value} is already assigned to Camper Group ID {existingGroup.groupId} in Camp ID {campId}.");
            }

            return (staff, camp);
        }

        #endregion
    }
}
