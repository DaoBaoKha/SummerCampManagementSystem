using System.ComponentModel.DataAnnotations;

namespace SummerCampManagementSystem.BLL.DTOs.AlbumPhoto
{
    public class AlbumPhotoRequestDto
    {
        [Required(ErrorMessage = "Album ID is required.")]
        public int? AlbumId { get; set; }

        [Required(ErrorMessage = "Photo URL/path cannot be empty.")]
        [StringLength(255)]
        public string Photo { get; set; }

        [StringLength(255)]
        public string Caption { get; set; }
    }
}
