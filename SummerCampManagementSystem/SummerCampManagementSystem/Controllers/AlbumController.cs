using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Album;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/album")]
    [ApiController]
    public class AlbumController : ControllerBase
    {
        private readonly IAlbumService _albumService;

        public AlbumController(IAlbumService albumService)
        {
            _albumService = albumService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAlbums()
        {
            var albums = await _albumService.GetAllAlbumsAsync();
            return Ok(albums); 
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAlbumById(int id)
        {
            var album = await _albumService.GetAlbumByIdAsync(id);
            if (album == null)
            {
                return NotFound($"Album with ID {id} not found."); 
            }
            return Ok(album); 
        }

        [HttpGet("camp/{campId}")]
        public async Task<IActionResult> GetAlbumsByCampId(int campId)
        {
            var albums = await _albumService.GetAlbumsByCampIdAsync(campId);
            return Ok(albums);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateAlbum([FromBody] AlbumRequestDto albumRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); 
            }
            try
            {
                var createdAlbum = await _albumService.CreateAlbumAsync(albumRequest);
                return CreatedAtAction(nameof(GetAlbumById), new { id = createdAlbum.AlbumId }, createdAlbum);
            }
            catch (ArgumentException ex) 
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex) 
            {
                return NotFound(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred during album creation.");
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateAlbum(int id, [FromBody] AlbumRequestDto albumRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var updatedAlbum = await _albumService.UpdateAlbumAsync(id, albumRequest);
                return Ok(updatedAlbum); 
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex) 
            {
                return NotFound(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the album.");
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteAlbum(int id)
        {
            try
            {
                var result = await _albumService.DeleteAlbumAsync(id);
                if (!result)
                {
                    return NotFound($"Album with ID {id} not found.");
                }
                return NoContent();
            }
            catch (ArgumentException ex) 
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the album.");
            }
        }
    }
}
