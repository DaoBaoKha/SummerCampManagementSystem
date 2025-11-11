using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.AlbumPhoto;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/albumPhoto")]
    public class AlbumPhotosController : ControllerBase
    {
        private readonly IAlbumPhotoService _albumPhotoService;

        public AlbumPhotosController(IAlbumPhotoService albumPhotoService)
        {
            _albumPhotoService = albumPhotoService;
        }


        [HttpPost]
        public async Task<IActionResult> AddPhoto([FromBody] AlbumPhotoRequestDto photoRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var newPhoto = await _albumPhotoService.CreatePhotoAsync(photoRequest);
                return CreatedAtAction(
                    nameof(GetPhotoById),
                    new { id = newPhoto.AlbumPhotoId },
                    newPhoto
                );
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetPhotoById(int id)
        {
            var photo = await _albumPhotoService.GetPhotoByIdAsync(id);

            if (photo == null)
            {
                return NotFound(new { message = $"Photo with ID {id} not found." });
            }

            return Ok(photo);
        }

        [HttpGet("album/{albumId}")]
        public async Task<IActionResult> GetPhotosByAlbum(int albumId)
        {
            var photos = await _albumPhotoService.GetPhotosByAlbumIdAsync(albumId);
            return Ok(photos);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePhoto(int id, [FromBody] AlbumPhotoRequestDto photoRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var updatedPhoto = await _albumPhotoService.UpdatePhotoAsync(id, photoRequest);
                return Ok(updatedPhoto);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int id)
        {
            try
            {
                var success = await _albumPhotoService.DeletePhotoAsync(id);
                if (!success)
                {
                    return NotFound(new { message = $"Photo with ID {id} not found." });
                }

                return NoContent();
            }
            catch (InvalidOperationException ex) 
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}