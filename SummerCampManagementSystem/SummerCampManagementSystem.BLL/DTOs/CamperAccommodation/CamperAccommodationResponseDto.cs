namespace SummerCampManagementSystem.BLL.DTOs.CamperAccommodation
{
    public class CamperAccommodationResponseDto
    {
        public int camperAccommodationId { get; set; }
        public int camperId { get; set; }
        public string? camperName { get; set; }
        public int accommodationId { get; set; }
        public string? accommodationName { get; set; }
        public int? campId { get; set; }
        public string? campName { get; set; }
        public string? status { get; set; }
    }
}
