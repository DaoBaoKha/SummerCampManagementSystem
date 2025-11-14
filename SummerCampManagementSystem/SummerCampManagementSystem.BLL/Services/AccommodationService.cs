using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.Accommodation;
using SummerCampManagementSystem.BLL.DTOs.CampStaffAssignment;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
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
            // check and validate supervisor
            await RunSupervisorValidation((int)accommodationRequestDto.supervisorId, accommodationRequestDto.campId);

            var accommodationEntity = _mapper.Map<Accommodation>(accommodationRequestDto);
            accommodationEntity.isActive = true;

            await _unitOfWork.Accommodations.CreateAsync(accommodationEntity);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<AccommodationResponseDto>(accommodationEntity);
        }

        public async Task<bool> DeactivateAccommodationAsync(int accommodationId)
        {
            var accommodation = await _unitOfWork.Accommodations.GetByIdAsync(accommodationId)
                ?? throw new KeyNotFoundException("Accommodation not found.");

            accommodation.isActive = false;
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

        public async Task<AccommodationResponseDto> GetBySupervisorIdAsync(int supervisorId, int campId)
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

            await RunSupervisorUpdateValidation(accommodationId, (int)newSupervisorId, (int)campId);

            _mapper.Map(accommodationRequestDto, existingAccommodation);
            await _unitOfWork.Accommodations.UpdateAsync(existingAccommodation);
            await _unitOfWork.CommitAsync();

            return _mapper.Map<AccommodationResponseDto>(existingAccommodation);
        }
        #region Private Methods

        private async Task RunSupervisorValidation(int supervisorId, int campId)
        {
            if (supervisorId == 0) throw new ArgumentException("SupervisorId must be provided and non-zero.");

            // check existence and role of the supervisor
            var staff = await _unitOfWork.Users.GetByIdAsync(supervisorId);
            if (staff == null)
            {
                throw new KeyNotFoundException($"Supervisor with ID {supervisorId} not found.");
            }

            if (staff.role != "Staff" && staff.role != "Manager")
            {
                throw new ArgumentException($"User with ID {supervisorId} is a '{staff.role}', not a Staff or Manager.");
            }

            // check if staff is assigned to the camp
            var isAssigned = await _campStaffAssignmentService.IsStaffAssignedToCampAsync(supervisorId, campId);
            if (!isAssigned)
            {
                // add staff to camp if not assigned
                var assignmentDto = new CampStaffAssignmentRequestDto { StaffId = supervisorId, CampId = campId };
                await _campStaffAssignmentService.AssignStaffToCampAsync(assignmentDto);
            }

            // staff only supervises one accommodation
            var existingAccommodation = await _unitOfWork.Accommodations.GetBySupervisorIdAsync(supervisorId, campId);
            if (existingAccommodation != null)
            {
                throw new InvalidOperationException($"Supervisor with ID {supervisorId} is already supervising accommodation ID {existingAccommodation.accommodationId}. Each staff can only supervise one accommodation.");
            }
        }


        private async Task RunSupervisorUpdateValidation(int accommodationId, int supervisorId, int campId)
        {
            if (supervisorId <= 0) throw new ArgumentException("SupervisorId must be provided and non-zero.");

            // check existence and role of the supervisor
            var staff = await _unitOfWork.Users.GetByIdAsync(supervisorId);
            if (staff == null)
            {
                throw new KeyNotFoundException($"Supervisor with ID {supervisorId} not found.");
            }

            if (staff.role != "Staff" && staff.role != "Manager")
            {
                throw new ArgumentException($"User with ID {supervisorId} is a '{staff.role}', not a Staff or Manager.");
            }

            // check if the supervisor is already assigned to another accommodation
            var existingAccommodation = await _unitOfWork.Accommodations.GetBySupervisorIdAsync(supervisorId, campId);

            // check if the existing accommodation is different from the current one being updated
            if (existingAccommodation != null && existingAccommodation.accommodationId != accommodationId)
            {
                throw new InvalidOperationException($"Supervisor with ID {supervisorId} is already supervising accommodation ID {existingAccommodation.accommodationId}. Each staff can only supervise one accommodation.");
            }

            // assign staff to camp if not already assigned
            var isAssigned = await _campStaffAssignmentService.IsStaffAssignedToCampAsync(supervisorId, campId);
            if (!isAssigned)
            {
                var assignmentDto = new CampStaffAssignmentRequestDto { StaffId = supervisorId, CampId = campId };
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
