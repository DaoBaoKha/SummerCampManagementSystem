using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;
using static SummerCampManagementSystem.BLL.DTOs.Location.LocationRequestDto;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/location")]
    [ApiController]
    [Authorize]
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


        [HttpGet("type")]
        public async Task<IActionResult> GetLocationsByType([FromQuery] LocationType type)
        {
            var locations = await _locationService.GetLocationsByTypeAsync(type);
            return Ok(locations);
        }

        // GET CHILD LOCATIONS BY PARENT ID (child location in camp) 
        /// <summary>
        /// take (In_camp) from a specific camp
        /// </summary>
        [HttpGet("parent/{parentLocationId}")]
        public async Task<IActionResult> GetChildLocationsByParentId(int parentLocationId)
        {
            var locations = await _locationService.GetChildLocationsByParentIdAsync(parentLocationId);
            return Ok(locations);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLocation([FromBody] LocationCreateDto locationDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var response = await _locationService.CreateLocationAsync(locationDto);
                return StatusCode(StatusCodes.Status201Created, response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLocation(int id, [FromBody] LocationUpdateDto locationDto)
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
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // cant update when LocationType of Parent has child Locations
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Lỗi hệ thống: {ex.Message}" });
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
            catch (InvalidOperationException ex)
            {
                // cant delete when Parent Location has Child Locations
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }
    }
}