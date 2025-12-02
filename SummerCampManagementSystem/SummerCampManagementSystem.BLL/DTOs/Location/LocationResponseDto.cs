using SummerCampManagementSystem.Core.Enums;

namespace SummerCampManagementSystem.BLL.DTOs.Location
{
    public class LocationResponseDto
    {
        public int LocationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public LocationType LocationType { get; set; }
        public bool IsActive { get; set; }
        public string? Address { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public int? ParentLocationId { get; set; }
        public string? ParentLocationName { get; set; }
    }

    public class LocationDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; 
    }

    public class LocationDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
}
