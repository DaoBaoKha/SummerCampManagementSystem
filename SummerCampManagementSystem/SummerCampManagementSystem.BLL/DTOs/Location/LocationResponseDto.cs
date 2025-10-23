namespace SummerCampManagementSystem.BLL.DTOs.Location
{
    public class LocationResponseDto
    {
        public int locationId { get; set; }
        public int? routeId { get; set; }
        public string Name { get; set; }
        public string locationType { get; set; }
        public bool isActive { get; set; }

    }

    public class LocationDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; 
    }
}
