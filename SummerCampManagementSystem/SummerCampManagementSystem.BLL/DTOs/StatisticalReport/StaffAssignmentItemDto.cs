namespace SummerCampManagementSystem.BLL.DTOs.StatisticalReport
{
    public class StaffAssignmentItemDto
    {
        public string StaffName { get; set; }
        public string Role { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string AssignmentType { get; set; } // Activity, Group, Transport, etc.
    }
}
