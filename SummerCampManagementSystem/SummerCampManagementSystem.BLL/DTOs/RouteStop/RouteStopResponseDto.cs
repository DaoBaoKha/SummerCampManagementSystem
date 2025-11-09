namespace SummerCampManagementSystem.BLL.DTOs.RouteStop
{
    public class RouteStopResponseDto
    {
        public int routeStopId { get; set; }
        public int RouteId { get; set; }
        public int LocationId { get; set; }
        public int StopOrder { get; set; }
        public int EstimatedTime { get; set; }
        public string Status { get; set; } = null!;
    }
}
