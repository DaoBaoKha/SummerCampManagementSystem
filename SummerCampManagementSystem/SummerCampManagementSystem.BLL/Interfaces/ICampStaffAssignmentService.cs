using SummerCampManagementSystem.BLL.DTOs.CampStaffAssignment;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface ICampStaffAssignmentService
    {
        Task<CampStaffAssignmentResponseDto> AssignStaffToCampAsync(CampStaffAssignmentRequestDto requestDto);

        Task<bool> DeleteAssignmentAsync(int assignmentId);

        Task<CampStaffAssignmentResponseDto?> GetAssignmentByIdAsync(int assignmentId);

        Task<IEnumerable<CampStaffAssignmentResponseDto>> GetAssignmentsByCampIdAsync(int campId);

        Task<IEnumerable<CampStaffSummaryDto>> GetAssignmentsByStaffIdAsync(int staffId);

        Task<bool> IsStaffAssignedToCampAsync(int staffId, int campId);
    }
}
