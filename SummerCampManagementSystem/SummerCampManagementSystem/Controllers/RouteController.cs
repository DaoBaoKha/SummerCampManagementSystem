using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Route;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/route")]
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

        [HttpGet("camp/{campId}")]
        public async Task<IActionResult> GetRouteByCampId(int campId)
        {
            var route = await _routeService.GetRoutesByCampIdAsync(campId);
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

        /// <summary>
        /// Create composite routes including optional return route
        /// </summary>
        /// <param name="requestDto"></param>
        /// <returns></returns>
        [HttpPost("composite")]
        public async Task<ActionResult<List<RouteResponseDto>>> CreateRouteComposite([FromBody] CreateRouteCompositeRequestDto requestDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var results = await _routeService.CreateRouteCompositeAsync(requestDto);

            return Ok(new
            {
                Message = $"Đã tạo thành công {results.Count} tuyến đường.",
                Data = results
            });
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
