namespace SummerCampManagementSystem.BLL.DTOs.Responses.Route
{
    public class RouteResponseDto
    {
        public int routeId { get; set; }
        public int campId { get; set; }
        public string CampName { get; set; }
        public string routeName { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
    }
}
