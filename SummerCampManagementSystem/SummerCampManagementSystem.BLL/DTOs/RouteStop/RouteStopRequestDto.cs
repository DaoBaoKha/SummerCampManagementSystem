using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.RouteStop
{
    public class RouteStopRequestDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "RouteId must be greater than 0.")]
        public int routeId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "LocationId must be greater than 0.")]
        public int locationId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "StopOrder must be greater than 0.")]
        public int stopOrder { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "EstimatedTime must be greater than 0.")]
        public int estimatedTime { get; set; }

    }
}
