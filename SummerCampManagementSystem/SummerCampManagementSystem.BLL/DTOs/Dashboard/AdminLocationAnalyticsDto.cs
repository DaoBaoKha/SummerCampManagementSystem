namespace SummerCampManagementSystem.BLL.DTOs.Dashboard
{
    public class AdminLocationAnalyticsDto
    {
        public List<LocationStatsDto> TopLocationsByCampCount { get; set; }
    }

    public class LocationStatsDto
    {
        public int LocationId { get; set; }
        public string Name { get; set; }
        public int CampCount { get; set; }
        public int ActiveCamps { get; set; }
    }
}
