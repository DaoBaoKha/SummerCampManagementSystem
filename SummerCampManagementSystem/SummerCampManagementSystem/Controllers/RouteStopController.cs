using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.RouteStop;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/routestop")]
    [ApiController]
    public class RouteStopController : ControllerBase
    {
        private readonly IRouteStopService _routeStopService;

        public RouteStopController(IRouteStopService routeStopService)
        {
            _routeStopService = routeStopService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var routeStops = await _routeStopService.GetAllRouteStopsAsync();
            return Ok(routeStops);
        }

        [HttpGet("{routeStopId}")]
        public async Task<IActionResult> GetRouteStopsById(int routeId)
        {
            var routeStops = await _routeStopService.GetRouteStopByIdAsync(routeId);

            return Ok(routeStops);
        }

        [HttpGet("route/{routeId}")]
        public async Task<IActionResult> GetRouteStopsByRouteId(int routeId)
        {
            var routeStops = await _routeStopService.GetRouteStopsByRouteIdAsync(routeId);

            if (routeStops == null || !routeStops.Any())
            {
                throw new NotFoundException(
                    $"No route stops found for route ID {routeId}."
                );
            }

            return Ok(routeStops);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRouteStop([FromBody] RouteStopRequestDto routeStopRequestDto)
        {
            if (!ModelState.IsValid)
                throw new BadRequestException("Invalid input data.");

            var newRouteStop = await _routeStopService.AddRouteStopAsync(routeStopRequestDto);

            return CreatedAtAction(nameof(GetRouteStopsById),
                new { newRouteStop.routeStopId }, newRouteStop
            );
        }

        [HttpPut("{routeStopId}")]
        public async Task<IActionResult> UpdateRouteStop(int routeStopId, [FromBody] RouteStopRequestDto routeStopRequestDto)
        {
            if (!ModelState.IsValid)
                throw new BadRequestException("Invalid input data.");

            var updatedRouteStop = await _routeStopService.UpdateRouteStopAsync(routeStopId, routeStopRequestDto);
            return Ok(updatedRouteStop);
        }

        [HttpDelete("{routeStopId}")]
        public async Task<IActionResult> DeleteRouteStop(int routeStopId)
        {
            await _routeStopService.DeleteRouteStopAsync(routeStopId);
            return NoContent();
        }
    }
}
