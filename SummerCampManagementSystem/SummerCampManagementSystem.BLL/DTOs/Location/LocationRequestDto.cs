namespace SummerCampManagementSystem.BLL.DTOs.Location
{
    public class LocationRequestDto
    {
        public string? Name { get; set; }
        public string? locationType { get; set; }
        public string? Address { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
}
