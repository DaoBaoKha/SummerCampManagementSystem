using AutoMapper;
using Microsoft.Extensions.Logging;
using SummerCampManagementSystem.BLL.DTOs.Group;
using SummerCampManagementSystem.BLL.Exceptions;
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
        private readonly ILogger<GroupService> _logger;

        public GroupService(IUnitOfWork unitOfWork, IValidationService validationService, IMapper mapper, ILogger<GroupService> logger)
        {
            _unitOfWork = unitOfWork;
            _validationService = validationService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<GroupResponseDto>> GetAllGroupsAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("[GroupService] GetAllGroupsAsync called");
            
            try
            {
                var groups = await _unitOfWork.Groups.GetAllAsync();
                
                stopwatch.Stop();
                _logger.LogInformation(
                    "[GroupService] Retrieved {Count} groups in {ElapsedMs}ms", 
                    groups.Count(), stopwatch.ElapsedMilliseconds);
                
                return _mapper.Map<IEnumerable<GroupResponseDto>>(groups);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[GroupService] ERROR in GetAllGroupsAsync - ElapsedMs={ElapsedMs}, Error={ErrorMessage}",
                    stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }

        public async Task<GroupWithCampDetailsResponseDto?> GetGroupBySupervisorIdAsync(int supervisorId, int campId)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("[GroupService] GetGroupBySupervisorIdAsync called with SupervisorId={SupervisorId}, CampId={CampId}", 
                supervisorId, campId);
            
            try
            {
                var camp = await _unitOfWork.Camps.GetByIdAsync(campId);
                if (camp == null)
                    throw new NotFoundException($"Camp with ID {campId} not found.");

                var group = await _unitOfWork.Groups.GetGroupBySupervisorIdAsync(supervisorId, campId);
                if (group == null)
                {
                    throw new NotFoundException($"Camper Group supervised by Staff ID {supervisorId} in Camp ID {campId} not found.");
                }
                
                stopwatch.Stop();
                _logger.LogInformation(
                    "[GroupService] Retrieved group for SupervisorId={SupervisorId}, CampId={CampId} in {ElapsedMs}ms",
                    supervisorId, campId, stopwatch.ElapsedMilliseconds);

                return _mapper.Map<GroupWithCampDetailsResponseDto>(group);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[GroupService] ERROR in GetGroupBySupervisorIdAsync - SupervisorId={SupervisorId}, CampId={CampId}, ElapsedMs={ElapsedMs}, Error={ErrorMessage}",
                    supervisorId, campId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }

        public async Task<GroupResponseDto?> GetGroupByIdAsync(int id)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("[GroupService] GetGroupByIdAsync called with GroupId={GroupId}", id);
            
            try
            {
                var Group = await _unitOfWork.Groups.GetByIdAsync(id)
                    ?? throw new NotFoundException($"Camper Group with ID {id} not found.");
                
                stopwatch.Stop();
                _logger.LogInformation(
                    "[GroupService] Retrieved group {GroupId} in {ElapsedMs}ms", 
                    id, stopwatch.ElapsedMilliseconds);
                
                return _mapper.Map<GroupResponseDto>(Group);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[GroupService] ERROR in GetGroupByIdAsync - GroupId={GroupId}, ElapsedMs={ElapsedMs}, Error={ErrorMessage}",
                    id, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }

        public async Task<GroupResponseDto> CreateGroupAsync(GroupRequestDto Group)
        {
            _logger.LogInformation("[GroupService] CreateGroupAsync called for CampId={CampId}", Group.CampId);
            
            try
            {
                await RunGroupSupervisorValidation(Group.SupervisorId, Group.CampId);

                var group = _mapper.Map<Group>(Group);

                await _unitOfWork.Groups.CreateAsync(group);

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
                
                _logger.LogInformation(
                    "[GroupService] Successfully created GroupId={GroupId} with {ActivityCount} activities", 
                    group.groupId, coreSchedules.Count());
                
                return _mapper.Map<GroupResponseDto>(group);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "[GroupService] ERROR in CreateGroupAsync - CampId={CampId}, Error={ErrorMessage}", 
                    Group.CampId, ex.Message);
                throw;
            }
        }

        public async Task<GroupResponseDto> AssignStaffToGroup(int GroupId, int staffId)
        {
            var group = await _unitOfWork.Groups.GetByIdAsync(GroupId)
                ?? throw new NotFoundException($"Camper Group with ID {GroupId} not found.");

            var campId = group.campId ?? throw new BusinessRuleException("Camper Group is not assigned to a Camp.");

            await RunGroupSupervisorValidation(staffId, campId, GroupId);

            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new NotFoundException("Camp not found."); 

            var staff = await _unitOfWork.Users.GetByIdAsync(staffId)
                ?? throw new NotFoundException("Staff not found."); 

            bool staffConflict = await _unitOfWork.ActivitySchedules
            .IsStaffBusyAsync(staffId, camp.startDate.Value, camp.endDate.Value);

            if (staffConflict)
                throw new BusinessRuleException("Staff has another activity scheduled during this time.");

            group.supervisorId = staffId;
            await _unitOfWork.Groups.UpdateAsync(group);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<GroupResponseDto>(group);
        }

        public async Task<GroupResponseDto?> UpdateGroupAsync(int id, GroupRequestDto Group)
        {
            var existingGroup = await _unitOfWork.Groups.GetByIdAsync(id)
                ?? throw new NotFoundException($"Camper Group with ID {id} not found.");

            await RunGroupSupervisorValidation(Group.SupervisorId, Group.CampId, id);

            _mapper.Map(Group, existingGroup);

            await _unitOfWork.Groups.UpdateAsync(existingGroup);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<GroupResponseDto>(existingGroup);
        }

        public async Task<bool> DeleteGroupAsync(int id)
        {
            _logger.LogInformation("[GroupService] DeleteGroupAsync called for GroupId={GroupId}", id);
            
            try
            {
                var existingGroup = await _unitOfWork.Groups.GetByIdAsync(id)
                    ?? throw new NotFoundException($"Camper Group with ID {id} not found."); 

                await _unitOfWork.Groups.RemoveAsync(existingGroup);

                var groupActivities = await _unitOfWork.GroupActivities.GetByGroupId(id);
                foreach (var ga in groupActivities)
                {
                    await _unitOfWork.GroupActivities.RemoveAsync(ga);
                }

                await _unitOfWork.CommitAsync();
                _logger.LogInformation(
                    "[GroupService] Successfully deleted GroupId={GroupId} and {ActivityCount} activities", 
                    id, groupActivities.Count());

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "[GroupService] ERROR in DeleteGroupAsync - GroupId={GroupId}, Error={ErrorMessage}", 
                    id, ex.Message);
                throw;
            }
        }

        public async Task<IEnumerable<GroupResponseDto>> GetGroupsByActivityScheduleId(int activityScheduleId)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("[GroupService] GetGroupsByActivityScheduleId called with ActivityScheduleId={ActivityScheduleId}", activityScheduleId);
            
            try
            {
                var activitySchedule = await _unitOfWork.ActivitySchedules.GetByIdAsync(activityScheduleId)
                    ?? throw new NotFoundException("Activity Schedule not found.");
                
                var groups = await _unitOfWork.Groups.GetGroupsByActivityScheduleIdAsync(activityScheduleId);
                
                stopwatch.Stop();
                _logger.LogInformation(
                    "[GroupService] Retrieved {Count} groups for ActivityScheduleId={ActivityScheduleId} in {ElapsedMs}ms",
                    groups.Count(), activityScheduleId, stopwatch.ElapsedMilliseconds);
                
                return _mapper.Map<IEnumerable<GroupResponseDto>>(groups);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[GroupService] ERROR in GetGroupsByActivityScheduleId - ActivityScheduleId={ActivityScheduleId}, ElapsedMs={ElapsedMs}, Error={ErrorMessage}",
                    activityScheduleId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }


        public async Task<IEnumerable<GroupResponseDto>> GetGroupsByCampIdAsync(int campId)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("[GroupService] GetGroupsByCampIdAsync called with CampId={CampId}", campId);
            
            try
            {
                var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                    ?? throw new NotFoundException($"Camp with ID {campId} not found.");

                var groups = await _unitOfWork.Groups.GetByCampIdAsync(campId);
                
                stopwatch.Stop();
                _logger.LogInformation(
                    "[GroupService] Successfully retrieved {Count} groups for CampId={CampId} in {ElapsedMs}ms", 
                    groups.Count(), campId, stopwatch.ElapsedMilliseconds);
                
                return _mapper.Map<IEnumerable<GroupResponseDto>>(groups);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, 
                    "[GroupService] ERROR in GetGroupsByCampIdAsync - CampId={CampId}, ElapsedMs={ElapsedMs}, Error={ErrorMessage}", 
                    campId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }

        #region Private Methods

        private async Task<(UserAccount staff, Camp camp)> RunGroupSupervisorValidation(int? supervisorId, int campId, int? groupId = null)
        {
            // if supervisorId is null or <= 0, skip validation and return camp only
            if (!supervisorId.HasValue || supervisorId.Value <= 0)
            {
                var campOnly = await _unitOfWork.Camps.GetByIdAsync(campId)
                    ?? throw new NotFoundException($"Camp with ID {campId} not found.");
                return (null, campOnly);
            }

            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new NotFoundException($"Camp with ID {campId} not found.");

            var staff = await _unitOfWork.Users.GetByIdAsync(supervisorId.Value)
                ?? throw new NotFoundException($"Supervisor with ID {supervisorId.Value} not found.");

            // check role
            if (!string.Equals(staff.role, "Staff", StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException($"User with ID {supervisorId.Value} is a '{staff.role}', not a Staff member.");
            }

            // check if staff is already assigned to another group in the same camp 
            var existingGroup = await _unitOfWork.Groups.GetGroupBySupervisorIdAsync(supervisorId.Value, campId);

            if (existingGroup != null && existingGroup.groupId != groupId)
            {
                throw new BusinessRuleException($"Supervisor ID {supervisorId.Value} is already assigned to Camper Group ID {existingGroup.groupId} in Camp ID {campId}.");
            }

            return (staff, camp);
        }

        #endregion
    }
}
