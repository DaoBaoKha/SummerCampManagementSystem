using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Location;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.BLL.Services;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/location")]
    [ApiController]
    public class LocationController : ControllerBase
    {
        private readonly ILocationService _locationService;

        public LocationController(ILocationService locationService)
        {
            _locationService = locationService;
        }


        [HttpGet]
        public async Task<IActionResult> GetAllLocations()
        {
            var locations = await _locationService.GetLocationsAsync();
            return Ok(locations);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLocationByIdAsync(int id)
        {
            var locations = await _locationService.GetLocationByIdAsync(id);
            if (locations == null) return NotFound();
            return Ok(locations);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLocation([FromBody] LocationRequestDto locationDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var response = await _locationService.CreateLocationAsync(locationDto);
                return StatusCode(StatusCodes.Status201Created, response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLocation(int id, [FromBody] LocationRequestDto locationDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var response = await _locationService.UpdateLocationAsync(id, locationDto);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLocation(int id)
        {
            try
            {
                var response = await _locationService.DeleteLocationAsync(id);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

    }
}
