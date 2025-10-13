using Google.Api;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Requests.Activity;
using SummerCampManagementSystem.BLL.Interfaces;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActivityController : ControllerBase
    {
        private readonly IActivityService _service;
        public ActivityController(IActivityService activityService)
        {
            _service = activityService;
        }
        // GET: api/<ActivityController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("camp/{campId}")]
        public async Task<IActionResult> GetByCampId(int campId)
        {
            var result = await _service.GetByCampIdAsync(campId);
            return Ok(result);
        }
   

        // GET api/<ActivityController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound();
            return Ok(result);
        }



        // POST api/<ActivityController>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ActivityCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.ActivityId }, result);
        }

        // PUT api/<ActivityController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ActivityCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var success = await _service.UpdateAsync(id, dto);
            return success ? NoContent() : NotFound();
        }

        // DELETE api/<ActivityController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            return success ? NoContent() : NotFound();
        }
    }
}
