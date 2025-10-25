namespace SummerCampManagementSystem.BLL.DTOs.Blog
{
    public class BlogRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime PublishedDate { get; set; }
    }
}
