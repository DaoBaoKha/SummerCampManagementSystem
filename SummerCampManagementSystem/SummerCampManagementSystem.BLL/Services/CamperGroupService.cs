using AutoMapper;
using SummerCampManagementSystem.BLL.DTOs.Camper;
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
            var groups = await _unitOfWork.CamperGroups.GetAllAsync();
            return _mapper.Map<IEnumerable<CamperGroupResponseDto>>(groups);
        }

        public async Task<CamperGroupWithCampDetailsResponseDto?> GetGroupBySupervisorIdAsync(int supervisorId, int campId)

        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId);
            if (camp == null)
                throw new KeyNotFoundException("Camp not found.");

            var group = await _unitOfWork.CamperGroups.GetGroupBySupervisorIdAsync(supervisorId, campId);
            return group == null ? null : _mapper.Map<CamperGroupWithCampDetailsResponseDto>(group);
        }

        public async Task<CamperGroupResponseDto?> GetCamperGroupByIdAsync(int id)
        {
           var camperGroup = await _unitOfWork.CamperGroups.GetByIdAsync(id);
           return camperGroup == null ? null : _mapper.Map<CamperGroupResponseDto>(camperGroup);    
        }

        public async Task<CamperGroupResponseDto> CreateCamperGroupAsync(CamperGroupRequestDto camperGroup)
        {
            await _validationService.ValidateEntityExistsAsync(camperGroup.CampId, _unitOfWork.Camps.GetByIdAsync, "Camp");

            var camp = await _unitOfWork.Camps.GetByIdAsync(camperGroup.CampId)
                ?? throw new KeyNotFoundException("Camp not found.");

            var group = _mapper.Map<CamperGroup>(camperGroup);

            await _unitOfWork.CamperGroups.CreateAsync(group);
            await _unitOfWork.CommitAsync();

            return  _mapper.Map<CamperGroupResponseDto>(group);
        }

        public async Task<CamperGroupResponseDto> AssignStaffToGroup(int camperGroupId, int staffId)
        {
            var group = await _unitOfWork.CamperGroups.GetByIdAsync(camperGroupId)
                ?? throw new KeyNotFoundException("Camper Group not found.");

            var camp = await _unitOfWork.Camps.GetByIdAsync(group.campId.Value);

            var staff = await _unitOfWork.Users.GetByIdAsync(staffId)
                ?? throw new KeyNotFoundException("Staff not found.");

            if (!string.Equals(staff.role, "Staff", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Assigned user is not a staff member.");
            }

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
            await _validationService.ValidateEntityExistsAsync(camperGroup.CampId, _unitOfWork.Camps.GetByIdAsync, "Camp");

            var existingCamperGroup = await _unitOfWork.CamperGroups.GetByIdAsync(id);

            if (existingCamperGroup == null) return null;

            _mapper.Map(camperGroup, existingCamperGroup);

            await _unitOfWork.CamperGroups.UpdateAsync(existingCamperGroup);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<CamperGroupResponseDto>(existingCamperGroup);
        }

        public async Task<bool> DeleteCamperGroupAsync(int id)
        {
            var existingCamperGroup = await _unitOfWork.CamperGroups.GetByIdAsync(id);

            if (existingCamperGroup == null) return false;

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
    }
}
