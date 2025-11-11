using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.AccommodationType
{
    public class AccommodationTypeRequestDto
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(50)]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(100)]
        public string? Description { get; set; }
    }
}
