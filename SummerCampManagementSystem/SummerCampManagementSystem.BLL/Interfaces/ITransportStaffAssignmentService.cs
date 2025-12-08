using SummerCampManagementSystem.BLL.DTOs.TransportStaffAssignment;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface ITransportStaffAssignmentService
    {
        Task<TransportStaffAssignmentResponseDto> AssignStaffAsync(TransportStaffAssignmentCreateDto dto);
        Task<TransportStaffAssignmentResponseDto> UpdateAssignmentAsync(int id, TransportStaffAssignmentUpdateDto dto);
        Task<IEnumerable<TransportStaffAssignmentResponseDto>> SearchAssignmentsAsync(TransportStaffAssignmentSearchDto searchDto);
        Task<bool> DeleteAssignmentAsync(int id);
    }
}
