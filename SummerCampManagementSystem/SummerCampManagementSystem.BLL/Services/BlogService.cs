using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.Blog;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

namespace SummerCampManagementSystem.BLL.Services
{
    public class BlogService : IBlogService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidationService _validationService;

        public BlogService(IUnitOfWork unitOfWork, IValidationService validationService)
        {
            _unitOfWork = unitOfWork;
            _validationService = validationService;
        }

        public async Task<BlogResponseDto> CreateBlogPostAsync(BlogRequestDto blogPost)
        {
            var author = await _unitOfWork.Users.GetByIdAsync(blogPost.AuthorId);
            if (author == null)
            {
                throw new KeyNotFoundException("Author not found.");
            }

            var newBlogPost = new Blog
            {
                title = blogPost.Title,
                content = blogPost.Content,
                authorId = blogPost.AuthorId,
                createAt = DateTime.UtcNow,
                isActive = true,
            };

            await _unitOfWork.Blogs.CreateAsync(newBlogPost);
            await _unitOfWork.CommitAsync();

            newBlogPost.author = author;

            return MapToBlogResponseDto(newBlogPost);
        }
        public async Task<bool> DeleteBlogPostAsync(int id)
        {
            var existingBlogPost = await _unitOfWork.Blogs.GetByIdAsync(id);
            if (existingBlogPost == null)
            {
                return false;
            }
            await _unitOfWork.Blogs.RemoveAsync(existingBlogPost);
            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task<IEnumerable<Blog>> GetAllBlogPostsAsync()
        {
            return await _unitOfWork.Blogs.GetAllAsync();
        }

        public async Task<BlogResponseDto?> GetBlogPostByIdAsync(int id)
        {
            var blogPost = await _unitOfWork.Blogs.GetQueryable()
                .Where(b => b.blogId == id)
                .Select(b => new BlogResponseDto
                {
                    Id = b.blogId,
                    Title = b.title,
                    Content = b.content,
                    CreatedAt = b.createAt ?? DateTime.MinValue,
                    AuthorId = b.authorId ?? 0,
                    AuthorName = b.author != null ? $"{b.author.firstName} {b.author.lastName}" : "N/A"
                })
                .FirstOrDefaultAsync();
            return blogPost;
        }

        public async Task<BlogResponseDto?> UpdateBlogPostAsync(int id, BlogRequestDto blogPost)
        {
            var existingBlogPost = await _unitOfWork.Blogs.GetByIdAsync(id);
            if (existingBlogPost == null)
            {
                return null;
            }

            var author = await _unitOfWork.Users.GetByIdAsync(blogPost.AuthorId);
            if (author == null)
            {
                throw new KeyNotFoundException("Author not found.");
            }

            existingBlogPost.title = blogPost.Title;
            existingBlogPost.content = blogPost.Content;
            existingBlogPost.authorId = blogPost.AuthorId;
            _unitOfWork.Blogs.UpdateAsync(existingBlogPost);

            await _unitOfWork.CommitAsync();

            existingBlogPost.author = author;

            return MapToBlogResponseDto(existingBlogPost);
        }

        private BlogResponseDto MapToBlogResponseDto(Blog blog)
        {
            return new BlogResponseDto
            {
                Id = blog.blogId,
                Title = blog.title,
                Content = blog.content,
                CreatedAt = blog.createAt ?? DateTime.MinValue,
                AuthorId = blog.authorId ?? 0,
                AuthorName = blog.author != null ? $"{blog.author.firstName} {blog.author.lastName}" : "N/A"
            };
        }
    }
}
