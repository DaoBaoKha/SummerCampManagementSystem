using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.Album
{
    public class AlbumRequestDto
    {
        [Required(ErrorMessage = "Camp ID is required to create an Album.")]
        [Range(1, int.MaxValue, ErrorMessage = "Camp ID must be a positive integer.")]
        public int? CampId { get; set; }

        [Required(ErrorMessage = "Album title is required.")]
        [StringLength(255, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 255 characters.")]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateOnly? Date { get; set; } = null;
    }
}
