using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.CamperActivity;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.BLL.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CamperActivityController : ControllerBase
    { 
        private readonly ICamperActivityService _service;
        public CamperActivityController(ICamperActivityService camperActivityService)
        {
            _service = camperActivityService;
        }
        // GET: api/<CamperActivityController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        // GET api/<CamperActivityController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        // POST api/<CamperActivityController>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CamperActivityCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.CamperActivityId }, result);
        }

        [HttpPost("register-optional")]
        public async Task<IActionResult> RegisterOptionalActivity([FromBody] CamperActivityCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _service.RegisterOptionalActivityAsync(dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        // PUT api/<CamperActivityController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CamperActivityUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var success = await _service.UpdateAsync(id, dto);
            return success ? NoContent() : NotFound();
        }

        // DELETE api/<CamperActivityController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            return success ? NoContent() : NotFound();
        }


    }
}
