namespace SummerCampManagementSystem.BLL.DTOs.AlbumPhoto
{
    public class AlbumPhotoResponseDto
    {
        public int AlbumPhotoId { get; set; }
        public int? AlbumId { get; set; }
        public string Photo { get; set; }
        public string Caption { get; set; }
    }
}
