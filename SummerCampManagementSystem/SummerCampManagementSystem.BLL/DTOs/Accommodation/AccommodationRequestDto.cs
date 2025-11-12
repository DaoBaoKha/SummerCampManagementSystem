using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.Accommodation
{
    public class AccommodationRequestDto
    {
        [Required]
        public int campId { get; set; }

        [Required]
        public int accommodationTypeId { get; set; }

        [Required]
        [StringLength(255)]
        public string name { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Capacity must be a positive integer.")]
        public int? capacity { get; set; }

        public int? supervisorId { get; set; }
    }
}
