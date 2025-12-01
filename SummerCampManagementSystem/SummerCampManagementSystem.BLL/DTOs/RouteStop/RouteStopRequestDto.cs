using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.RouteStop
{
    public class RouteStopRequestDto
    {
        [Required]
        public int routeId { get; set; }

        [Required]
        public int locationId { get; set; }

        [Required]
        public int stopOrder { get; set; }

        [Required]
        public int? estimatedTime { get; set; }

    }
}
