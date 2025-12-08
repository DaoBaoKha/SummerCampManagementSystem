namespace SummerCampManagementSystem.BLL.DTOs.TransportStaffAssignment
{
    public class TransportStaffAssignmentCreateDto
    {
        public int TransportScheduleId { get; set; }
        public int StaffId { get; set; }
    }

    public class TransportStaffAssignmentUpdateDto
    {
        public int? TransportScheduleId { get; set; }
        public int? StaffId { get; set; } 
    }

    public class TransportStaffAssignmentSearchDto
    {
        public int? TransportScheduleId { get; set; }
        public int? StaffId { get; set; }
        public string? Status { get; set; }
    }

    public class TransportStaffAssignmentResponseDto
    {
        public int Id { get; set; }
        public int TransportScheduleId { get; set; }
        public int StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}