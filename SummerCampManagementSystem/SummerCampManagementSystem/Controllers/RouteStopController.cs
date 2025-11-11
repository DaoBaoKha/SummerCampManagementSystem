using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.RouteStop;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RouteStopController : ControllerBase
    {
        private readonly IRouteStopService _routeStopService;

        public RouteStopController(IRouteStopService routeStopService)
        {
            _routeStopService = routeStopService;
        }

        [HttpGet("{routeId}")]
        public async Task<IActionResult> GetRouteStopsByRouteId(int routeId)
        {
            try
            {
                var routeStops = await _routeStopService.GetRouteStopsByRouteIdAsync(routeId);

                if (routeStops == null || !routeStops.Any())
                {
                    return NotFound(new { message = $"No route stops found for route ID {routeId}." });
                }

                return Ok(routeStops);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An internal error occurred: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateRouteStop([FromBody] RouteStopRequestDto routeStopRequestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var newRouteStop = await _routeStopService.AddRouteStopAsync(routeStopRequestDto);
                return CreatedAtAction(nameof(GetRouteStopsByRouteId), new { routeId = newRouteStop.RouteId }, newRouteStop);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("{routeStopId}")]
        public async Task<IActionResult> UpdateRouteStop(int routeStopId, [FromBody] RouteStopRequestDto routeStopRequestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var updatedRouteStop = await _routeStopService.UpdateRouteStopAsync(routeStopId, routeStopRequestDto);
                return Ok(updatedRouteStop);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{routeStopId}")]
        public async Task<IActionResult> DeleteRouteStop(int routeStopId)
        {
            try
            {
                var result = await _routeStopService.DeleteRouteStopAsync(routeStopId);
                if (result)
                {
                    return NoContent();
                }
                else
                {
                    return NotFound(new { message = $"Route stop with ID {routeStopId} not found." });
                }
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
