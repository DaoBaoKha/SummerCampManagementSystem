namespace SummerCampManagementSystem.BLL.DTOs.Route
{
    public class RouteResponseDto
    {
        public int routeId { get; set; }
        public int campId { get; set; }
        public string CampName { get; set; }
        public string routeName { get; set; } = string.Empty;
        public string routeType { get; set; } = string.Empty;
        public int estimateDuration { get; set; }
        public bool isActive { get; set; }

        public string status { get; set; } = string.Empty;
    }

    public class RouteNameDto
    {
        public int routeId { get; set; }
        public string routeName { get; set; } = string.Empty;
    }
}
