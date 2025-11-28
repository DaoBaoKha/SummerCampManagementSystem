using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.CamperGroup;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class CamperGroupService : ICamperGroupService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidationService _validationService;
        private readonly IMapper _mapper;
        public CamperGroupService(IUnitOfWork unitOfWork, IValidationService validationService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _validationService = validationService;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CamperGroupResponseDto>> GetAllCamperGroupsAsync()
        {
            var groups = await _unitOfWork.CamperGroups.GetAllCamperGroups();
            return _mapper.Map<IEnumerable<CamperGroupResponseDto>>(groups);
        }

        public async Task<CamperGroupWithCampDetailsResponseDto?> GetGroupBySupervisorIdAsync(int supervisorId, int campId)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId);
            if (camp == null)
                throw new KeyNotFoundException($"Camp with ID {campId} not found.");

            var group = await _unitOfWork.CamperGroups.GetGroupBySupervisorIdAsync(supervisorId, campId);

            if (group == null)
            {
                throw new KeyNotFoundException($"Camper Group supervised by Staff ID {supervisorId} in Camp ID {campId} not found.");
            }

            return _mapper.Map<CamperGroupWithCampDetailsResponseDto>(group);
        }

        public async Task<CamperGroupResponseDto?> GetCamperGroupByIdAsync(int id)
        {
            var camperGroup = await _unitOfWork.CamperGroups.GetCamperGroupById(id)
                ?? throw new KeyNotFoundException($"Camper Group with ID {id} not found.");

            return camperGroup == null ? null : _mapper.Map<CamperGroupResponseDto>(camperGroup);
        }

        public async Task<CamperGroupResponseDto> CreateCamperGroupAsync(CamperGroupRequestDto camperGroup)
        {
            await RunGroupSupervisorValidation(camperGroup.SupervisorId, camperGroup.CampId);

            var group = _mapper.Map<CamperGroup>(camperGroup);


            await _unitOfWork.CamperGroups.CreateAsync(group);
            await _unitOfWork.CommitAsync();

            var coreSchedules = _unitOfWork.ActivitySchedules.GetCoreScheduleByCampIdAsync(camperGroup.CampId);

            foreach (var core in  coreSchedules.Result)
            {
                var groupActivity = new GroupActivity
                {
                    camperGroupId = group.camperGroupId,
                    activityScheduleId = core.activityScheduleId
                };
                await _unitOfWork.GroupActivities.CreateAsync(groupActivity);
            }

            await _unitOfWork.CommitAsync();
            
            return _mapper.Map<CamperGroupResponseDto>(group);
        }

        public async Task<CamperGroupResponseDto> AssignStaffToGroup(int camperGroupId, int staffId)
        {
            var group = await _unitOfWork.CamperGroups.GetByIdAsync(camperGroupId)
                ?? throw new KeyNotFoundException($"Camper Group with ID {camperGroupId} not found.");

            var campId = group.campId ?? throw new InvalidOperationException("Camper Group is not assigned to a Camp.");

            await RunGroupSupervisorValidation(staffId, campId, camperGroupId);

            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new KeyNotFoundException("Camp not found."); 

            var staff = await _unitOfWork.Users.GetByIdAsync(staffId)
                ?? throw new KeyNotFoundException("Staff not found."); 

            bool staffConflict = await _unitOfWork.ActivitySchedules
            .IsStaffBusyAsync(staffId, camp.startDate.Value, camp.endDate.Value);

            if (staffConflict)
                throw new InvalidOperationException("Staff has another activity scheduled during this time.");

            group.supervisorId = staffId;
            await _unitOfWork.CamperGroups.UpdateAsync(group);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<CamperGroupResponseDto>(group);
        }

        public async Task<CamperGroupResponseDto?> UpdateCamperGroupAsync(int id, CamperGroupRequestDto camperGroup)
        {
            var existingCamperGroup = await _unitOfWork.CamperGroups.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Camper Group with ID {id} not found.");

            await RunGroupSupervisorValidation(camperGroup.SupervisorId, camperGroup.CampId, id);

            _mapper.Map(camperGroup, existingCamperGroup);

            await _unitOfWork.CamperGroups.UpdateAsync(existingCamperGroup);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<CamperGroupResponseDto>(existingCamperGroup);
        }

        public async Task<bool> DeleteCamperGroupAsync(int id)
        {
            var existingCamperGroup = await _unitOfWork.CamperGroups.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Camper Group with ID {id} not found."); 

            await _unitOfWork.CamperGroups.RemoveAsync(existingCamperGroup);
            await _unitOfWork.CommitAsync();

            return true;
        }

        public async Task<IEnumerable<CamperGroupResponseDto>> GetGroupsByActivityScheduleId(int activityScheduleId)
        {
            var activitySchedule = await _unitOfWork.ActivitySchedules.GetByIdAsync(activityScheduleId)
                ?? throw new KeyNotFoundException("Activity Schedule not found.");
            var groups = await _unitOfWork.CamperGroups.GetGroupsByActivityScheduleIdAsync(activityScheduleId);
            return _mapper.Map<IEnumerable<CamperGroupResponseDto>>(groups);
        }


        public async Task<IEnumerable<CamperGroupResponseDto>> GetGroupsByCampIdAsync(int campId)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new KeyNotFoundException($"Camp with ID {campId} not found.");

            var groups = await _unitOfWork.CamperGroups.GetQueryable()
                .Include(g => g.supervisor)
                .Where(g => g.campId == campId)
                .ToListAsync();

            return _mapper.Map<IEnumerable<CamperGroupResponseDto>>(groups);
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
            var existingGroup = await _unitOfWork.CamperGroups.GetGroupBySupervisorIdAsync(supervisorId.Value, campId);

            if (existingGroup != null && existingGroup.camperGroupId != groupId)
            {
                throw new InvalidOperationException($"Supervisor ID {supervisorId.Value} is already assigned to Camper Group ID {existingGroup.camperGroupId} in Camp ID {campId}.");
            }

            return (staff, camp);
        }

        #endregion
    }
}
