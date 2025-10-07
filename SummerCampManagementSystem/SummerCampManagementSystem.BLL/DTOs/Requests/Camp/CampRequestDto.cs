namespace SummerCampManagementSystem.BLL.DTOs.Requests.Camp
{
    public class CampRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Place { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty; 
        public int MinParticipants { get; set; } = 0;
        public int MaxParticipants { get; set; } = 0;
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        public string image { get; set; } = string.Empty;
        public int? CampTypeId { get; set; }
        public int? LocationId { get; set; } = null;
        public decimal Price { get; set; } = 0; 
    }
}
