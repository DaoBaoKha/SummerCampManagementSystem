using SummerCampManagementSystem.BLL.DTOs.CampStaffAssignment;
using SummerCampManagementSystem.BLL.DTOs.UserAccount;
using SummerCampManagementSystem.DAL.Models;

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
        Task<IEnumerable<StaffSummaryDto>> GetAvailableStaffManagerByCampIdAsync(int campId);
        Task<IEnumerable<StaffSummaryDto>> GetAvailableStaffByCampId(int campId);
    }
}
