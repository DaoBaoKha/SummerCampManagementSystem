namespace SummerCampManagementSystem.BLL.DTOs.AlbumPhoto
{
    public class BulkAlbumPhotoResponseDto
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<AlbumPhotoResponseDto> SuccessfulPhotos { get; set; } = new();
        public List<BulkUploadError> Errors { get; set; } = new();
    }

    public class BulkUploadError
    {
        public string FileName { get; set; }
        public string ErrorMessage { get; set; }
    }
}
