using SummerCampManagementSystem.BLL.DTOs.Blog;
using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IBlogService
    {
        Task<IEnumerable<Blog>> GetAllBlogPostsAsync();
        Task<BlogResponseDto?> GetBlogPostByIdAsync(int id);
        Task<BlogResponseDto> CreateBlogPostAsync(BlogRequestDto blogPost);
        Task<BlogResponseDto?> UpdateBlogPostAsync(int id, BlogRequestDto blogPost);
        Task<bool> DeleteBlogPostAsync(int id);
    }
}
