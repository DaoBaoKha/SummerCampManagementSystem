using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.CamperAccommodation
{
    public class CamperAccommodationRequestDto
    {
        [Required]
        public int? camperId { get; set; }

        [Required]
        public int? accommodationId { get; set; }
    }

    public class CamperAccommodationSearchDto
    {
        public int? CamperId { get; set; }
        public int? AccommodationId { get; set; }
        public int? CampId { get; set; }
        public string? CamperName { get; set; }
    }
}
