using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.AlbumPhoto;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/album-photo")]
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

            var newPhoto = await _albumPhotoService.CreatePhotoAsync(photoRequest);
            return CreatedAtAction(
                nameof(GetPhotoById),
                new { id = newPhoto.AlbumPhotoId },
                newPhoto
            );
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

            var updatedPhoto = await _albumPhotoService.UpdatePhotoAsync(id, photoRequest);
            return Ok(updatedPhoto);
        }


        [HttpPost("bulk")]
        public async Task<IActionResult> AddMultiplePhotos([FromForm] int albumId, [FromForm] List<IFormFile> photos, [FromForm] string? caption = null)
        {
            // validation for formdata
            if (photos == null || photos.Count == 0)
            {
                return BadRequest(new { message = "At least one photo is required." });
            }

            if (photos.Count > 20)
            {
                return BadRequest(new { message = "Maximum 20 photos allowed per upload." });
            }

            var result = await _albumPhotoService.CreateMultiplePhotosAsync(albumId, photos, caption);

            // return appropriate status based on results
            if (result.FailureCount == 0)
            {
                return Ok(new
                {
                    message = $"Successfully uploaded {result.SuccessCount} photos.",
                    data = result
                });
            }
            else if (result.SuccessCount == 0)
            {
                return BadRequest(new
                {
                    message = "All photos failed to upload.",
                    data = result
                });
            }
            else
            {
                // partial success
                return Ok(new
                {
                    message = $"Uploaded {result.SuccessCount} photos successfully, {result.FailureCount} failed.",
                    data = result
                });
            }
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int id)
        {
            var success = await _albumPhotoService.DeletePhotoAsync(id);
            if (!success)
            {
                return NotFound(new { message = $"Photo with ID {id} not found." });
            }

            return NoContent();
        }
    }
}