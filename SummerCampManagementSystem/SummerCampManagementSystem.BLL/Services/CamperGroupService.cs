using AutoMapper;
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

        public async Task<CamperGroupResponseDto?> GetCamperGroupByIdAsync(int id)
        {
           var camperGroup = await _unitOfWork.CamperGroups.GetByIdAsync(id);
           return camperGroup == null ? null : _mapper.Map<CamperGroupResponseDto>(camperGroup);    
        }

        public async Task<CamperGroupResponseDto> CreateCamperGroupAsync(CamperGroupRequestDto camperGroup)
        {
            //await _validationService.ValidateEntityExistsAsync(camperGroup.SupervisorId, _unitOfWork.Users.GetByIdAsync, "Supervisor");
           // await _validationService.ValidateEntityExistsAsync(camperGroup.CampId, _unitOfWork.Camps.GetByIdAsync, "Camp");

            var camp = await _unitOfWork.Camps.GetByIdAsync(camperGroup.CampId)
                ?? throw new KeyNotFoundException("Camp not found.");


            if (camperGroup.SupervisorId > 0)
            {
                var staff = await _unitOfWork.Users.GetByIdAsync(camperGroup.SupervisorId)
                    ?? throw new KeyNotFoundException("Staff not found.");


                if (!string.Equals(staff.role, "Staff", StringComparison.OrdinalIgnoreCase))

                {
                    throw new InvalidOperationException("Assigned user is not a staff member.");
                }

                bool staffConflict = await _unitOfWork.ActivitySchedules
                   .IsStaffBusyAsync(camperGroup.SupervisorId, camp.startDate.Value.ToDateTime(TimeOnly.MinValue), camp.endDate.Value.ToDateTime(TimeOnly.MinValue));

                if (staffConflict)
                    throw new InvalidOperationException("Staff has another activity scheduled during this time.");
            }
            var group = _mapper.Map<CamperGroup>(camperGroup);

            await _unitOfWork.CamperGroups.CreateAsync(group);
            await _unitOfWork.CommitAsync();

            return  _mapper.Map<CamperGroupResponseDto>(group);
        }

        public async Task<CamperGroupResponseDto?> UpdateCamperGroupAsync(int id, CamperGroupRequestDto camperGroup)
        {
            await _validationService.ValidateEntityExistsAsync(camperGroup.SupervisorId, _unitOfWork.Users.GetByIdAsync, "Supervisor");
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
    }
}
