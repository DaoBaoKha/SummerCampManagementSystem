using SummerCampManagementSystem.BLL.DTOs.Requests.CamperGroup;
using SummerCampManagementSystem.BLL.DTOs.Responses.CamperGroup;
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
        public CamperGroupService(IUnitOfWork unitOfWork, IValidationService validationService)
        {
            _unitOfWork = unitOfWork;
            _validationService = validationService;
        }

        public async Task<IEnumerable<CamperGroup>> GetAllCamperGroupsAsync()
        {
            return await _unitOfWork.CamperGroups.GetAllAsync();
        }

        public async Task<CamperGroupResponseDto?> GetCamperGroupByIdAsync(int id)
        {
            var camperGroup = await _unitOfWork.CamperGroups.GetByIdAsync(id);
            if (camperGroup == null) return null;

            return new CamperGroupResponseDto
            {
                CamperGroupId = camperGroup.camperGroupId,
                GroupName = camperGroup.groupName,
                Description = camperGroup.description,
                MaxSize = (int)camperGroup.maxSize,
                SupervisorId = (int)camperGroup.supervisorId,
                CampId = (int)camperGroup.campId
            };
        }

        public async Task<CamperGroupResponseDto> CreateCamperGroupAsync(CamperGroupRequestDto camperGroup)
        {
            await _validationService.ValidateEntityExistsAsync(camperGroup.SupervisorId, _unitOfWork.Users.GetByIdAsync, "Supervisor");
            await _validationService.ValidateEntityExistsAsync(camperGroup.CampId, _unitOfWork.Camps.GetByIdAsync, "Camp");

            var newCamperGroup = new CamperGroup
            {
                groupName = camperGroup.GroupName,
                description = camperGroup.Description,
                maxSize = camperGroup.MaxSize,
                supervisorId = camperGroup.SupervisorId,
                campId = camperGroup.CampId
            };

            await _unitOfWork.CamperGroups.CreateAsync(newCamperGroup);
            await _unitOfWork.CommitAsync();

            return new CamperGroupResponseDto 
            {
                CamperGroupId = newCamperGroup.camperGroupId,
                GroupName = newCamperGroup.groupName,
                Description = newCamperGroup.description,
                MaxSize = (int)newCamperGroup.maxSize,
                SupervisorId = (int)newCamperGroup.supervisorId,
                CampId = (int)newCamperGroup.campId
            };
        }

        public async Task<CamperGroupResponseDto?> UpdateCamperGroupAsync(int id, CamperGroupRequestDto camperGroup)
        {
            await _validationService.ValidateEntityExistsAsync(camperGroup.SupervisorId, _unitOfWork.Users.GetByIdAsync, "Supervisor");
            await _validationService.ValidateEntityExistsAsync(camperGroup.CampId, _unitOfWork.Camps.GetByIdAsync, "Camp");

            var existingCamperGroup = await _unitOfWork.CamperGroups.GetByIdAsync(id);

            if (existingCamperGroup == null) return null;

            existingCamperGroup.groupName = camperGroup.GroupName;
            existingCamperGroup.description = camperGroup.Description;
            existingCamperGroup.maxSize = camperGroup.MaxSize;
            existingCamperGroup.supervisorId = camperGroup.SupervisorId;
            existingCamperGroup.campId = camperGroup.CampId;

            await _unitOfWork.CamperGroups.UpdateAsync(existingCamperGroup);
            await _unitOfWork.CommitAsync();

            return new CamperGroupResponseDto
            {
                CamperGroupId = existingCamperGroup.camperGroupId,
                GroupName = existingCamperGroup.groupName,
                Description = existingCamperGroup.description,
                MaxSize = (int)existingCamperGroup.maxSize,
                SupervisorId = (int)existingCamperGroup.supervisorId,
                CampId = (int)existingCamperGroup.campId
            };
        }

        public async Task<bool> DeleteCamperGroupAsync(int id)
        {
            var existingCamperGroup = await _unitOfWork.CamperGroups.GetByIdAsync(id);

            if (existingCamperGroup == null) return false;

            await _unitOfWork.CamperGroups.RemoveAsync(existingCamperGroup);
            await _unitOfWork.CommitAsync();

            return true;
        }
    }
}
