using AutoMapper;
using AutoMapper.QueryableExtensions; 
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.CampStaffAssignment;
using SummerCampManagementSystem.BLL.DTOs.UserAccount;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class CampStaffAssignmentService : ICampStaffAssignmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CampStaffAssignmentService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        private async Task<(UserAccount staff, Camp camp)> RunValidationChecks(int staffId, int campId)
        {
            var staff = await _unitOfWork.Users.GetByIdAsync(staffId);
            if (staff == null)
            {
                throw new KeyNotFoundException($"Staff with ID {staffId} not found.");
            }

            if (staff.role != "Staff" && staff.role != "Manager")
            {
                throw new ArgumentException($"User with ID {staffId} is a '{staff.role}', not a Staff or Manager.");
            }

            var camp = await _unitOfWork.Camps.GetByIdAsync(campId);
            if (camp == null)
            {
                throw new KeyNotFoundException($"Camp with ID {campId} not found.");
            }

            return (staff, camp);
        }


        public async Task<CampStaffAssignmentResponseDto> AssignStaffToCampAsync(CampStaffAssignmentRequestDto requestDto)
        {
            await RunValidationChecks(requestDto.StaffId, requestDto.CampId);

            // check duplicate
            var existingAssignment = await _unitOfWork.CampStaffAssignments.GetQueryable()
                .FirstOrDefaultAsync(csa =>
                    csa.staffId == requestDto.StaffId &&
                    csa.campId == requestDto.CampId);

            if (existingAssignment != null)
            {
                throw new ArgumentException("This staff member is already assigned to this camp.");
            }

            var newAssignment = _mapper.Map<CampStaffAssignment>(requestDto);

            await _unitOfWork.CampStaffAssignments.CreateAsync(newAssignment);
            await _unitOfWork.CommitAsync();

            var createdAssignmentDto = await _unitOfWork.CampStaffAssignments.GetQueryable()
                .Where(csa => csa.campStaffAssignmentId == newAssignment.campStaffAssignmentId)
                .ProjectTo<CampStaffAssignmentResponseDto>(_mapper.ConfigurationProvider)
                .FirstAsync(); 

            return createdAssignmentDto;
        }

        public async Task<bool> DeleteAssignmentAsync(int assignmentId)
        {
            var assignment = await _unitOfWork.CampStaffAssignments.GetByIdAsync(assignmentId);
            if (assignment == null)
            {
                throw new KeyNotFoundException($"Assignment with ID {assignmentId} not found.");
            }

            await _unitOfWork.CampStaffAssignments.RemoveAsync(assignment);
            await _unitOfWork.CommitAsync();
            return true;
        }


        public async Task<CampStaffAssignmentResponseDto?> GetAssignmentByIdAsync(int assignmentId)
        {
            var assignment = await _unitOfWork.CampStaffAssignments.GetQueryable()
                .Where(csa => csa.campStaffAssignmentId == assignmentId)
                .ProjectTo<CampStaffAssignmentResponseDto>(_mapper.ConfigurationProvider) 
                .FirstOrDefaultAsync();

            return assignment; 
        }


        public async Task<IEnumerable<CampStaffAssignmentResponseDto>> GetAssignmentsByCampIdAsync(int campId)
        {
            var assignments = await _unitOfWork.CampStaffAssignments.GetQueryable()
                .Where(csa => csa.campId == campId)
                .ProjectTo<CampStaffAssignmentResponseDto>(_mapper.ConfigurationProvider) 
                .ToListAsync();

            return assignments; 
        }


        public async Task<IEnumerable<CampStaffSummaryDto>> GetAssignmentsByStaffIdAsync(int staffId)
        {
            var assignments = await _unitOfWork.CampStaffAssignments.GetQueryable()
                .Where(csa => csa.staffId == staffId)
                .ProjectTo<CampStaffSummaryDto>(_mapper.ConfigurationProvider) 
                .ToListAsync();

            return assignments; 
        }

        public async Task<bool> IsStaffAssignedToCampAsync(int staffId, int campId)
        {
            var existingAssignment = await _unitOfWork.CampStaffAssignments.GetQueryable()
                .FirstOrDefaultAsync(csa =>
                    csa.staffId == staffId &&
                    csa.campId == campId);

            return existingAssignment != null;
        }

        public async Task<IEnumerable<StaffSummaryDto>> GetAvailableStaffByCampIdAsync(int campId)
        {
            var camp = await _unitOfWork.Camps.GetByIdAsync(campId)
                ?? throw new KeyNotFoundException("Camp not found.");

           var availableStaffs = await _unitOfWork.CampStaffAssignments
                .GetAvailableStaffByCampIdAsync(camp.startDate, camp.endDate);

            return _mapper.Map<IEnumerable<StaffSummaryDto>>(availableStaffs);
        }
    }
}
