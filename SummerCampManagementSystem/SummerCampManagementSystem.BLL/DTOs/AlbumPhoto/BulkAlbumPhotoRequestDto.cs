using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.AlbumPhoto
{
    public class BulkAlbumPhotoRequestDto
    {
        [Required(ErrorMessage = "Album ID is required.")]
        public int AlbumId { get; set; }

        [Required(ErrorMessage = "At least one photo is required.")]
        [MinLength(1, ErrorMessage = "At least one photo is required.")]
        [MaxLength(20, ErrorMessage = "Maximum 20 photos allowed per upload.")]
        public List<Microsoft.AspNetCore.Http.IFormFile> Photos { get; set; }

        [StringLength(255)]
        public string? DefaultCaption { get; set; }
    }
}
