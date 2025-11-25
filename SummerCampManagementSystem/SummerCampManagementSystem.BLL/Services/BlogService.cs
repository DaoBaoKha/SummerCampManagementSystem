using AutoMapper;
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
        private readonly IUserContextService _userContextService;
        private readonly IUploadSupabaseService _uploadSupabaseService;

        public BlogService(IUnitOfWork unitOfWork, IUserContextService userContextService, IUploadSupabaseService uploadSupabaseService)
        {
            _unitOfWork = unitOfWork;
            _userContextService = userContextService;
            _uploadSupabaseService = uploadSupabaseService;
        }

        private void RunBlogValidationChecks(BlogRequestDto blogPost)
        {
            if (string.IsNullOrWhiteSpace(blogPost.Title))
            {
                throw new ArgumentException("The blog post title cannot be empty.");
            }

            if (blogPost.Title.Length > 255) 
            {
                throw new ArgumentException("The blog post title cannot exceed 255 characters.");
            }

            if (string.IsNullOrWhiteSpace(blogPost.Content))
            {
                throw new ArgumentException("The blog post content cannot be empty.");
            }

            if (blogPost.Content.Length < 10)
            {
                throw new ArgumentException("The blog post content must be at least 10 characters long.");
            }
        }

        public async Task<BlogResponseDto> CreateBlogPostAsync(BlogRequestDto blogPost)
        {
            RunBlogValidationChecks(blogPost);

            var authorId = _userContextService.GetCurrentUserId();
            if (authorId == null)
            {
                throw new UnauthorizedAccessException("Cannot create blog post. Author information is invalid or missing.");
            }

            var author = await _unitOfWork.Users.GetByIdAsync(authorId.Value);
            if (author == null)
            {
                throw new KeyNotFoundException("Author account not found in the system.");
            }

            var newBlogPost = new Blog
            {
                title = blogPost.Title,
                content = blogPost.Content,
                authorId = authorId, 
                createAt = DateTime.UtcNow,
                isActive = true,
            };

            await _unitOfWork.Blogs.CreateAsync(newBlogPost);
            await _unitOfWork.CommitAsync();

            if (blogPost.ImageUrl != null)
            {
                var url = await _uploadSupabaseService.UploadBlogImageAsync(newBlogPost.blogId, blogPost.ImageUrl);
                newBlogPost.imageUrl = url;
            }

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

            // only the original author can delete
            var currentUserId = _userContextService.GetCurrentUserId();
            if (currentUserId == null || existingBlogPost.authorId != currentUserId.Value)
            {
                throw new UnauthorizedAccessException("You do not have permission to delete this post. Only the original author can delete");
            }

            await _unitOfWork.Blogs.RemoveAsync(existingBlogPost);
            await _unitOfWork.CommitAsync();
            return true;
        }

        public async Task<IEnumerable<BlogResponseDto>> GetAllBlogPostsAsync()
        {
            // use Include() to load Author and project to DTO
            var blogs = await _unitOfWork.Blogs.GetQueryable()
                .Include(b => b.author) 
                .Select(b => new BlogResponseDto
                {
                    Id = b.blogId,
                    Title = b.title,
                    Content = b.content,
                    CreatedAt = b.createAt ?? DateTime.MinValue,
                    AuthorId = b.authorId ?? 0,
                    AuthorName = b.author != null ? $"{b.author.firstName} {b.author.lastName}" : "N/A"
                })
                .ToListAsync();

            return blogs;
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

            var currentUserId = _userContextService.GetCurrentUserId();
            if (currentUserId == null || existingBlogPost.authorId != currentUserId.Value)
            {
                throw new UnauthorizedAccessException("You do not have permission to edit this post. Only original author and edit");
            }

            RunBlogValidationChecks(blogPost);

            var author = await _unitOfWork.Users.GetByIdAsync(existingBlogPost.authorId.Value);

            existingBlogPost.title = blogPost.Title;
            existingBlogPost.content = blogPost.Content;

            await _unitOfWork.Blogs.UpdateAsync(existingBlogPost);

            if (blogPost.ImageUrl != null)
            {
                var url = await _uploadSupabaseService.UploadBlogImageAsync(existingBlogPost.blogId, blogPost.ImageUrl);
                existingBlogPost.imageUrl = url;
            }
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
                ImageUrl = blog.imageUrl,
                AuthorId = blog.authorId ?? 0,
                AuthorName = blog.author != null ? $"{blog.author.lastName} {blog.author.firstName}" : "N/A"
            };
        }
    }
}