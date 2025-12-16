using SummerCampManagementSystem.BLL.DTOs.RouteStop;
using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.Route
{
    public class RouteRequestDto
    {
        public int campId { get; set; }
        public string routeName { get; set; } = string.Empty;
        public string routeType { get; set; } = string.Empty;
        public int estimateDuration { get; set; }
    }

    public class CreateRouteCompositeRequestDto
    {
        [Required(ErrorMessage = "Camp ID là bắt buộc.")]
        public int CampId { get; set; }

        [Required(ErrorMessage = "Tên tuyến đường là bắt buộc")]
        public string RouteName { get; set; } = string.Empty;

        public string RouteType { get; set; } = "PickUp";

        public int EstimateDuration { get; set; }

        [Required(ErrorMessage = "Danh sách điểm dừng là bắt buộc.")]
        [MinLength(2, ErrorMessage = "Tuyến đường phải có ít nhất 2 điểm dừng.")]
        public List<RouteStopRequestDto> RouteStops { get; set; } = new List<RouteStopRequestDto>();

        public bool CreateReturnRoute { get; set; } = false;

        public string? ReturnRouteName { get; set; }
    }
}
