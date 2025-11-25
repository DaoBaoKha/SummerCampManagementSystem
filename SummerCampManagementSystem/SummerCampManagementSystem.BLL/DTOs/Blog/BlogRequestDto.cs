using Microsoft.AspNetCore.Http;

namespace SummerCampManagementSystem.BLL.DTOs.Blog
{
    public class BlogRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public IFormFile? ImageUrl { get; set; }
    }
}
