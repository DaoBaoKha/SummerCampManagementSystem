namespace SummerCampManagementSystem.BLL.DTOs.Report
{
    public class TransportIncidentRequestDto
    {
        public int camperId { get; set; }
        
        public int transportScheduleId { get; set; }
        
        public string note { get; set; } = string.Empty;

        public string? imageUrl { get; set; }
    }
}
