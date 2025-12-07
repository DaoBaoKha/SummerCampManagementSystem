using SummerCampManagementSystem.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.Location
{
    public class LocationRequestDto
    {
        public class LocationCreateDto
        {
            [Required(ErrorMessage = "Tên vị trí là bắt buộc.")]
            [StringLength(255, ErrorMessage = "Tên không được vượt quá 255 ký tự.")]
            public string? Name { get; set; }

            [Required(ErrorMessage = "Loại vị trí là bắt buộc.")]
            public LocationType LocationType { get; set; } 

            public string? Address { get; set; }
            public decimal? Latitude { get; set; }
            public decimal? Longitude { get; set; }

            // only use this when LocationType = In_camp
            public int? ParentLocationId { get; set; }
        }

        public class LocationUpdateDto
        {
            [StringLength(255, ErrorMessage = "Tên không được vượt quá 255 ký tự.")]
            public string? Name { get; set; }

            public LocationType? LocationType { get; set; }
            public bool? IsActive { get; set; } // admin or staff can deactivate location
            public string? Address { get; set; }
            public decimal? Latitude { get; set; }
            public decimal? Longitude { get; set; }

            public int? ParentLocationId { get; set; }
        }
    }
}
