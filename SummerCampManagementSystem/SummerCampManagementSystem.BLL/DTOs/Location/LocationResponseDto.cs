namespace SummerCampManagementSystem.BLL.DTOs.Location
{
    public class LocationResponseDto
    {
        public int locationId { get; set; }
        public string Name { get; set; }
        public string locationType { get; set; }
        public bool isActive { get; set; }
        public string? Address { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

    }

    public class LocationDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; 
    }
}
