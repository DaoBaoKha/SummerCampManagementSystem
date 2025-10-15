using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Blog;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/blog")]
    [ApiController]
    public class BlogController : ControllerBase
    {
        private readonly IBlogService _blogService;

        public BlogController(IBlogService blogService)
        {
            _blogService = blogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBlogPosts()
        {
            var blogPosts = await _blogService.GetAllBlogPostsAsync();
            return Ok(blogPosts);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBlogPostById(int id)
        {
            var blogPost = await _blogService.GetBlogPostByIdAsync(id);
            if (blogPost == null) return NotFound();
            return Ok(blogPost);
        }

        [HttpPost]
        public async Task<IActionResult> CreateBlogPost([FromBody] BlogRequestDto blogPost)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var createdBlogPost = await _blogService.CreateBlogPostAsync(blogPost);
            return CreatedAtAction(nameof(GetAllBlogPosts), new { id = createdBlogPost.Id }, createdBlogPost);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBlogPost(int id)
        {
            var result = await _blogService.DeleteBlogPostAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBlogPost(int id, [FromBody] BlogRequestDto blogPost)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var updatedBlogPost = await _blogService.UpdateBlogPostAsync(id, blogPost);
            if (updatedBlogPost == null)
            {
                return NotFound();
            }
            return Ok(updatedBlogPost);
        }

    }
}
