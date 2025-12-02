using SummerCampManagementSystem.BLL.DTOs.Location;
using SummerCampManagementSystem.BLL.DTOs.Route;

namespace SummerCampManagementSystem.BLL.DTOs.RouteStop
{
    public class RouteStopResponseDto
    {
        public int routeStopId { get; set; }
        public RouteNameDto Route { get; set; }
        public LocationDetailDto Location { get; set; }
        public int stopOrder { get; set; }
        public int estimatedTime { get; set; }
        public string status { get; set; } = null!;
    }
}
