namespace SummerCampManagementSystem.BLL.DTOs.Report
{
    public class IncidentTicketRequestDto
    {
        public int camperId { get; set; }
        
        public int? activityScheduleId { get; set; }
        
        public int level { get; set; }
        
        public string note { get; set; } = string.Empty;
        
        public string? imageUrl { get; set; }
    }
}
