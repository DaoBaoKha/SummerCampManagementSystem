using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Livestream;
using SummerCampManagementSystem.BLL.DTOs.Report;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.BLL.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LiveStreamController : ControllerBase
    {
        private readonly ILiveStreamService _liveStreamService;
        private readonly IUserContextService _userContextService;   
        public LiveStreamController(ILiveStreamService liveStreamService, IUserContextService userContextService)
        {
            _liveStreamService = liveStreamService;
            _userContextService = userContextService;
        }
        // GET: api/<LiveStreamController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var livestreams = await _liveStreamService.GetAllLiveStreamsAsync();
            return Ok(livestreams);
        }

        [HttpGet]
        [Route("by-date-range")]
        public async Task<IActionResult> GetByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var livestreams = await _liveStreamService.GetLiveStreamsByDateRangeAsync(startDate, endDate);
            return Ok(livestreams);
        }

        // GET api/<LiveStreamController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var livestream = await _liveStreamService.GetLiveStreamByIdAsync(id);
            if (livestream == null) return NotFound();
            return Ok(livestream);
        }

        // POST api/<LiveStreamController>
        [Authorize(Roles = "Staff")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LivestreamRequestDto dto)
        {
            var staffId = _userContextService.GetCurrentUserId();
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var created = await _liveStreamService.CreateLiveStreamAsync(dto, staffId.Value);
            return CreatedAtAction(nameof(GetById), new { id = created.livestreamId }, created);
        }

        // PUT api/<LiveStreamController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] LivestreamRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var updated = await _liveStreamService.UpdateLiveStreamAsync(id, dto);
            if (updated == null)
            {
                return NotFound();
            }
            return Ok(updated);
        }

        // DELETE api/<LiveStreamController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _liveStreamService.DeleteLiveStreamAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}
