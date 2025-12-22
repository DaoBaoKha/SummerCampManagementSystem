using AutoMapper;
using Microsoft.Extensions.Logging;
using SummerCampManagementSystem.BLL.DTOs.Group;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
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
                // use GetAllCamperGroups which filters only Active groups
                var groups = await _unitOfWork.Groups.GetAllCamperGroups();
                
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
            var memoryBefore = GC.GetTotalMemory(false) / 1024 / 1024;
            
            _logger.LogInformation(
                "[GroupService] CRITICAL ENDPOINT - GetGroupBySupervisorIdAsync called - SupervisorId={SupervisorId}, CampId={CampId}, MemoryBefore={MemoryMB}MB, ThreadId={ThreadId}", 
                supervisorId, campId, memoryBefore, Environment.CurrentManagedThreadId);
            
            try
            {
                // Validate input parameters
                if (supervisorId <= 0)
                {
                    _logger.LogWarning("[GroupService] Invalid SupervisorId={SupervisorId}", supervisorId);
                    throw new BadRequestException($"Invalid supervisor ID: {supervisorId}");
                }
                
                if (campId <= 0)
                {
                    _logger.LogWarning("[GroupService] Invalid CampId={CampId}", campId);
                    throw new BadRequestException($"Invalid camp ID: {campId}");
                }

                _logger.LogInformation("[GroupService] Fetching camp with CampId={CampId}", campId);
                var camp = await _unitOfWork.Camps.GetByIdAsync(campId);
                if (camp == null)
                {
                    _logger.LogWarning("[GroupService] Camp not found - CampId={CampId}", campId);
                    throw new NotFoundException($"Camp with ID {campId} not found.");
                }

                _logger.LogInformation("[GroupService] Camp found - CampId={CampId}, CampName={CampName}. Fetching group...", 
                    campId, camp.name);
                    
                var group = await _unitOfWork.Groups.GetGroupBySupervisorIdAsync(supervisorId, campId);
                
                if (group == null)
                {
                    _logger.LogWarning(
                        "[GroupService] Group NOT FOUND - SupervisorId={SupervisorId}, CampId={CampId}",
                        supervisorId, campId);
                    throw new NotFoundException($"Camper Group supervised by Staff ID {supervisorId} in Camp ID {campId} not found.");
                }
                
                _logger.LogInformation(
                    "[GroupService] Group found - GroupId={GroupId}, GroupName={GroupName}. Mapping to DTO...",
                    group.groupId, group.groupName);
                    
                var result = _mapper.Map<GroupWithCampDetailsResponseDto>(group);
                
                stopwatch.Stop();
                var memoryAfter = GC.GetTotalMemory(false) / 1024 / 1024;
                
                _logger.LogInformation(
                    "[GroupService] CRITICAL ENDPOINT SUCCESS - GetGroupBySupervisorIdAsync completed - SupervisorId={SupervisorId}, CampId={CampId}, GroupId={GroupId}, ElapsedMs={ElapsedMs}, MemoryBefore={MemoryBeforeMB}MB, MemoryAfter={MemoryAfterMB}MB, MemoryDelta={MemoryDeltaMB}MB",
                    supervisorId, campId, group.groupId, stopwatch.ElapsedMilliseconds, memoryBefore, memoryAfter, memoryAfter - memoryBefore);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var memoryAfter = GC.GetTotalMemory(false) / 1024 / 1024;
                
                _logger.LogError(ex,
                    "[GroupService] CRITICAL ERROR in GetGroupBySupervisorIdAsync - SupervisorId={SupervisorId}, CampId={CampId}, ElapsedMs={ElapsedMs}, MemoryBefore={MemoryBeforeMB}MB, MemoryAfter={MemoryAfterMB}MB, ExceptionType={ExceptionType}, Error={ErrorMessage}, StackTrace={StackTrace}",
                    supervisorId, campId, stopwatch.ElapsedMilliseconds, memoryBefore, memoryAfter, ex.GetType().Name, ex.Message, ex.StackTrace);
                throw;
            }
        }

        public async Task<GroupResponseDto?> GetGroupByIdAsync(int id)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("[GroupService] GetGroupByIdAsync called with GroupId={GroupId}", id);
            
            try
            {
                var Group = await _unitOfWork.Groups.GetByIdWithCamperGroupsAndCampAsync(id)
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
                // Validate camp status before creating group
                var camp = await _unitOfWork.Camps.GetByIdAsync(Group.CampId)
                    ?? throw new NotFoundException($"Camp with ID {Group.CampId} not found.");
                
                ValidateCampStatusForGroupOperation(camp, "tạo");

                await RunGroupSupervisorValidation(Group.SupervisorId, Group.CampId);

                var group = _mapper.Map<Group>(Group);

                await _unitOfWork.Groups.CreateAsync(group);
                await _unitOfWork.CommitAsync();

                var schedules = await _unitOfWork.ActivitySchedules.GetCheckInOutScheduleByCampIdAsync(Group.CampId);

                foreach (var schedule in schedules)
                {
                    var groupActivity = new GroupActivity
                    {
                        groupId = group.groupId,
                        activityScheduleId = schedule.activityScheduleId
                    };
                    await _unitOfWork.GroupActivities.CreateAsync(groupActivity);
                }

                await _unitOfWork.CommitAsync();
                
                _logger.LogInformation(
                    "[GroupService] Successfully created GroupId={GroupId} with {ActivityCount} activities", 
                    group.groupId, schedules.Count());
                var newGroup = await _unitOfWork.Groups.GetByIdWithCamperGroupsAndCampAsync(group.groupId);
                return _mapper.Map<GroupResponseDto>(newGroup);
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
            var existingGroup = await _unitOfWork.Groups.GetByIdWithCamperGroupsAndCampAsync(id)
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
                var existingGroup = await _unitOfWork.Groups.GetByIdWithCampAsync(id)
                    ?? throw new NotFoundException($"Camper Group with ID {id} not found."); 

                // check if camp status is published or higher
                var camp = await _unitOfWork.Camps.GetByIdAsync(existingGroup.campId.Value);
                if (camp != null && Enum.TryParse<CampStatus>(camp.status, out var campStatus) && campStatus >= CampStatus.Published)
                {
                    throw new BusinessRuleException("Không thể xóa nhóm khi trại đã được xuất bản.");
                }

                // soft delete group: set status to Inactive
                existingGroup.status = "Inactive";
                await _unitOfWork.Groups.UpdateAsync(existingGroup);

                // hard delete GroupActivities
                var groupActivities = await _unitOfWork.GroupActivities.GetByGroupId(id);
                foreach (var ga in groupActivities)
                {
                    await _unitOfWork.GroupActivities.RemoveAsync(ga);
                }

                await _unitOfWork.CommitAsync();
                
                _logger.LogInformation(
                    "[GroupService] Successfully soft deleted GroupId={GroupId} (status=Inactive) and removed {ActivityCount} activities", 
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

        private void ValidateCampStatusForGroupOperation(Camp camp, string operation)
        {
            var campStatus = camp.status;

            // Block operations if camp status is Published or later
            if (campStatus == CampStatus.Published.ToString() ||
                campStatus == CampStatus.OpenForRegistration.ToString() ||
                campStatus == CampStatus.RegistrationClosed.ToString() ||
                campStatus == CampStatus.UnderEnrolled.ToString() ||
                campStatus == CampStatus.InProgress.ToString() ||
                campStatus == CampStatus.Completed.ToString() ||
                campStatus == CampStatus.Canceled.ToString())
            {
                throw new BadRequestException($"Không thể {operation} group khi trại đã ở trạng thái '{campStatus}'. Trại phải ở trạng thái Draft, PendingApproval, hoặc Rejected.");
            }
        }

        private async Task<(UserAccount staff, Camp camp)> RunGroupSupervisorValidation(int? supervisorId, int campId, int? groupId = null)
        {
            _logger.LogInformation(
                "[GroupService] RunGroupSupervisorValidation called - SupervisorId={SupervisorId}, CampId={CampId}, GroupId={GroupId}",
                supervisorId, campId, groupId);
                
            try
            {
                // if supervisorId is null or <= 0, skip validation and return camp only
                if (!supervisorId.HasValue || supervisorId.Value <= 0)
                {
                    _logger.LogInformation("[GroupService] Skipping supervisor validation (no supervisor assigned)");
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
                    _logger.LogWarning(
                        "[GroupService] Role validation failed - UserId={UserId}, Role={Role}",
                        supervisorId.Value, staff.role);
                    throw new BadRequestException($"User with ID {supervisorId.Value} is a '{staff.role}', not a Staff member.");
                }

                // check if staff is already assigned to another group in the same camp 
                var existingGroup = await _unitOfWork.Groups.GetGroupBySupervisorIdAsync(supervisorId.Value, campId);

                if (existingGroup != null && existingGroup.groupId != groupId)
                {
                    _logger.LogWarning(
                        "[GroupService] Supervisor already assigned - SupervisorId={SupervisorId}, ExistingGroupId={ExistingGroupId}, CampId={CampId}",
                        supervisorId.Value, existingGroup.groupId, campId);
                    throw new BusinessRuleException($"Supervisor ID {supervisorId.Value} is already assigned to Camper Group ID {existingGroup.groupId} in Camp ID {campId}.");
                }

                _logger.LogInformation(
                    "[GroupService] Supervisor validation passed - SupervisorId={SupervisorId}, CampId={CampId}",
                    supervisorId.Value, campId);
                    
                return (staff, camp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[GroupService] ERROR in RunGroupSupervisorValidation - SupervisorId={SupervisorId}, CampId={CampId}, Error={ErrorMessage}",
                    supervisorId, campId, ex.Message);
                throw;
            }
        }

        #endregion
    }
}
