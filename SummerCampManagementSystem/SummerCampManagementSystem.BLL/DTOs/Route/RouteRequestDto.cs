namespace SummerCampManagementSystem.BLL.DTOs.Route
{
    public class RouteRequestDto
    {
        public int campId { get; set; }
        public string routeName { get; set; } = string.Empty;
        public string routeType { get; set; } = string.Empty;
        public int estimateDuration { get; set; }
        public bool isActive { get; set; }
    }
}
