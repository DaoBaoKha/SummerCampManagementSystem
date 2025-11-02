using SummerCampManagementSystem.BLL.DTOs.Camp;
using SummerCampManagementSystem.BLL.DTOs.UserAccount;

namespace SummerCampManagementSystem.BLL.DTOs.CampStaffAssignment
{
    public class CampStaffAssignmentResponseDto
    {
        public int CampStaffAssignmentId { get; set; }
        public StaffSummaryDto Staff { get; set; }
        public CampSummaryDto Camp { get; set; }
    }
}
