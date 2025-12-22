using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.Accommodation;
using SummerCampManagementSystem.BLL.DTOs.CampStaffAssignment;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Services
{
    public class AccommodationService : IAccommodationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IUserContextService _userContextService;
        private readonly ICampStaffAssignmentService _campStaffAssignmentService;

        public AccommodationService(IUnitOfWork unitOfWork, IMapper mapper, 
            IUserContextService userContextService, ICampStaffAssignmentService campStaffAssignmentService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userContextService = userContextService;
            _campStaffAssignmentService = campStaffAssignmentService;
        }

        public async Task<AccommodationResponseDto> CreateAccommodationAsync(AccommodationRequestDto accommodationRequestDto)
        {
            // Validate camp status before creating accommodation
            var camp = await _unitOfWork.Camps.GetByIdAsync(accommodationRequestDto.campId)
                ?? throw new KeyNotFoundException($"Camp with ID {accommodationRequestDto.campId} not found.");
            
            ValidateCampStatusForAccommodationOperation(camp, "tạo");

            // check and validate supervisor
            await RunSupervisorValidation(accommodationRequestDto.supervisorId, accommodationRequestDto.campId);
            
            var accommodationEntity = _mapper.Map<Accommodation>(accommodationRequestDto);
            
            accommodationEntity.isActive = true; 

            await _unitOfWork.Accommodations.CreateAsync(accommodationEntity);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<AccommodationResponseDto>(accommodationEntity);
        }

        public async Task<bool> UpdateAccommodationStatusAsync(int accommodationId, bool isActive)
        {
            var accommodation = await _unitOfWork.Accommodations.GetByIdAsync(accommodationId)
                ?? throw new KeyNotFoundException("Accommodation not found.");
            accommodation.isActive = isActive;
            await _unitOfWork.Accommodations.UpdateAsync(accommodation);
            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task<AccommodationResponseDto?> GetAccommodationByIdAsync(int accommodationId)
        {
            var accommodation = await GetAccommodationsWithIncludes()
                .FirstOrDefaultAsync(a => a.accommodationId == accommodationId)
                ?? throw new KeyNotFoundException("Accommodation not found.");

            return _mapper.Map<AccommodationResponseDto>(accommodation);
        }

        public async Task<IEnumerable<AccommodationResponseDto>> GetAccommodationsByCampIdAsync(int campId)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new KeyNotFoundException("Camp not found.");

            var accommodations = await GetAccommodationsWithIncludes()
                .Where(a => a.campId == campId)
                .ToListAsync();

            return _mapper.Map<IEnumerable<AccommodationResponseDto>>(accommodations);
        }

        public async Task<IEnumerable<AccommodationResponseDto>> GetAllAccommodationsAsync()
        {
            var accommodations = await GetAccommodationsWithIncludes().ToListAsync();

            return _mapper.Map<IEnumerable<AccommodationResponseDto>>(accommodations);
        }

        public async Task<AccommodationResponseDto?> GetBySupervisorIdAsync(int supervisorId, int campId)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new KeyNotFoundException($"Camp with ID {campId} not found.");

            var accommodation = await _unitOfWork.Accommodations.GetBySupervisorIdAsync(supervisorId, campId);

            if (accommodation == null)
            {
                throw new KeyNotFoundException($"Accommodation supervised by Staff ID {supervisorId} in Camp ID {campId} not found.");
            }

            return _mapper.Map<AccommodationResponseDto>(accommodation);
        }

        public async Task<AccommodationResponseDto> UpdateAccommodationAsync(int accommodationId, AccommodationRequestDto accommodationRequestDto)
        {
            var existingAccommodation = await _unitOfWork.Accommodations.GetByIdAsync(accommodationId)
                ?? throw new KeyNotFoundException("Accommodation not found.");

            // take new supervisorId and campId
            var campId = existingAccommodation.campId;
            var newSupervisorId = accommodationRequestDto.supervisorId;

            existingAccommodation.isActive = true; // ensure accommodation remains active
            
            await RunSupervisorUpdateValidation(accommodationId, newSupervisorId, campId.Value);
            
            _mapper.Map(accommodationRequestDto, existingAccommodation);
            await _unitOfWork.Accommodations.UpdateAsync(existingAccommodation);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<AccommodationResponseDto>(existingAccommodation);
        }

        public async Task<bool> DeleteAccommodationAsync(int accommodationId)
        {
            var accommodation = await _unitOfWork.Accommodations.GetByIdWithCampAsync(accommodationId)
                ?? throw new NotFoundException("Không tìm thấy chỗ ở.");

            // only delete when camp status is before Published
            var camp = await _unitOfWork.Camps.GetByIdAsync(accommodation.campId.Value);
            if (camp != null && Enum.TryParse<CampStatus>(camp.status, out var campStatus) && campStatus >= CampStatus.Published)
            {
                throw new BusinessRuleException("Không thể xóa chỗ ở khi trại đã được xuất bản.");
            }

            // soft delete set isActive = false 
            accommodation.isActive = false;
            accommodation.supervisorId = null;  // set null supervisor to release staff
            await _unitOfWork.Accommodations.UpdateAsync(accommodation);

            // Hard delete các AccommodationActivitySchedule liên quan
            var accommodationActivities = await _unitOfWork.AccommodationActivities
                .GetByAccommodationIdAsync(accommodationId);

            foreach (var accommodationActivity in accommodationActivities)
            {
                await _unitOfWork.AccommodationActivities.RemoveAsync(accommodationActivity);
            }

            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task<IEnumerable<AccommodationResponseDto>> GetActiveAccommodationsAsync()
        {
            var accommodations = await GetAccommodationsWithIncludes()
                .Where(a => a.isActive == true)
                .ToListAsync();

            return _mapper.Map<IEnumerable<AccommodationResponseDto>>(accommodations);
        }
        #region Private Methods

        private void ValidateCampStatusForAccommodationOperation(Camp camp, string operation)
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
                throw new BadRequestException($"Không thể {operation} accommodation khi trại đã ở trạng thái '{campStatus}'. Trại phải ở trạng thái Draft, PendingApproval, hoặc Rejected.");
            }
        }

        private async Task RunSupervisorValidation(int? supervisorId, int campId)
        {
            if (!supervisorId.HasValue) 
                return;

            if (supervisorId.Value == 0) 
                throw new ArgumentException("Supervisor ID cannot be zero.");

            int id = supervisorId.Value;

            // check existence and role of the supervisor
            var staff = await _unitOfWork.Users.GetByIdAsync(id);
            if (staff == null)
            {
                throw new KeyNotFoundException($"Supervisor with ID {supervisorId} not found.");
            }

            if (staff.role != "Staff" && staff.role != "Manager")
            {
                throw new ArgumentException($"User with ID {supervisorId} is a '{staff.role}', not a Staff or Manager.");
            }

            // check if staff is assigned to the camp
            var isAssigned = await _campStaffAssignmentService.IsStaffAssignedToCampAsync(id, campId);
            if (!isAssigned)
            {
                // add staff to camp if not assigned
                var assignmentDto = new CampStaffAssignmentRequestDto { StaffId = id, CampId = campId };
                await _campStaffAssignmentService.AssignStaffToCampAsync(assignmentDto);
            }

            // staff only supervises one accommodation
            var existingAccommodation = await _unitOfWork.Accommodations.GetBySupervisorIdAsync(id, campId);
            if (existingAccommodation != null)
            {
                throw new InvalidOperationException($"Supervisor with ID {supervisorId} is already supervising accommodation ID {existingAccommodation.accommodationId}. Each staff can only supervise one accommodation.");
            }
        }


        private async Task RunSupervisorUpdateValidation(int accommodationId, int? supervisorId, int campId)
        {
            if (!supervisorId.HasValue)
                return;

            if (supervisorId.Value == 0)
                throw new ArgumentException("Supervisor ID cannot be zero.");

            int id = supervisorId.Value;

            // check existence and role of the supervisor
            var staff = await _unitOfWork.Users.GetByIdAsync(id);
            if (staff == null)
            {
                throw new KeyNotFoundException($"Supervisor with ID {supervisorId} not found.");
            }

            if (staff.role != "Staff" && staff.role != "Manager")
            {
                throw new ArgumentException($"User with ID {supervisorId} is a '{staff.role}', not a Staff or Manager.");
            }

            // check if the supervisor is already assigned to another accommodation
            var existingAccommodation = await _unitOfWork.Accommodations.GetBySupervisorIdAsync(id, campId);

            // check if the existing accommodation is different from the current one being updated
            if (existingAccommodation != null && existingAccommodation.accommodationId != accommodationId)
            {
                throw new InvalidOperationException($"Supervisor with ID {supervisorId} is already supervising accommodation ID {existingAccommodation.accommodationId}. Each staff can only supervise one accommodation.");
            }

            // assign staff to camp if not already assigned
            var isAssigned = await _campStaffAssignmentService.IsStaffAssignedToCampAsync(id, campId);
            if (!isAssigned)
            {
                var assignmentDto = new CampStaffAssignmentRequestDto { StaffId = id, CampId = campId };
                await _campStaffAssignmentService.AssignStaffToCampAsync(assignmentDto);
            }
        }

        private IQueryable<Accommodation> GetAccommodationsWithIncludes()
        {
            return _unitOfWork.Accommodations.GetQueryable()
                .Include(a => a.supervisor); 
                                             
        }
        #endregion
    }
}
