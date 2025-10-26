namespace SummerCampManagementSystem.BLL.DTOs.Album
{
    public class AlbumResponseDto
    {
        public int AlbumId { get; set; }
        public int CampId { get; set; }
        public DateOnly? Date { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string CampName { get; set; } = string.Empty;

        public int PhotoCount { get; set; }
    }
}
