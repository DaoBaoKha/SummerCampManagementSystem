using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.CampStaffAssignment
{
    public class CampStaffAssignmentRequestDto
    {
        [Required(ErrorMessage = "Staff ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Staff ID must be a positive number.")]
        public int StaffId { get; set; }

        [Required(ErrorMessage = "Camp ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Camp ID must be a positive number.")]
        public int CampId { get; set; }
    }
}
