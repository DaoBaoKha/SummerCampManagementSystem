using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Requests.Route;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RouteController : ControllerBase
    {
        private readonly IRouteService _routeService;

        public RouteController(IRouteService routeService)
        {
            _routeService = routeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRoutes()
        {
            var routes = await _routeService.GetAllRoutesAsync();
            return Ok(routes);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRouteById(int id)
        {
            var route = await _routeService.GetRouteByIdAsync(id);
            if (route == null)
            {
                return NotFound();
            }
            return Ok(route);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoute([FromBody] RouteRequestDto routeRequestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var createdRoute = await _routeService.CreateRouteAsync(routeRequestDto);
            return CreatedAtAction(nameof(GetRouteById), new { id = createdRoute.routeId }, createdRoute);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRoute(int id, [FromBody] RouteRequestDto routeRequestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var updatedRoute = await _routeService.UpdateRouteAsync(id, routeRequestDto);
                return Ok(updatedRoute);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoute(int id)
        {
            try
            {
                await _routeService.DeleteRouteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }
    }
}
