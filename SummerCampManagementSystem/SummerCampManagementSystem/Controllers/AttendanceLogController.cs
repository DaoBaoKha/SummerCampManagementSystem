using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.AttendanceLog;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Repositories.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceLogController : ControllerBase
    {

        private readonly IAttendanceLogService _attendanceLogService;
        public AttendanceLogController(IAttendanceLogService attendanceLogService)
        {
            _attendanceLogService = attendanceLogService;
        }
        
        // GET: api/<AttendanceLogController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _attendanceLogService.GetAllAttendanceLogsAsync();
            return Ok(result);
        }

        // GET api/<AttendanceLogController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _attendanceLogService.GetAttendanceLogByIdAsync(id);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        // POST api/<AttendanceLogController>
        [HttpPost]
        [Route("core-activity")]

        public async Task<IActionResult> Create([FromBody] AttendanceLogRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _attendanceLogService.CoreActivityAttendanceAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.AttendanceLogId }, result);
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

        [HttpPost]
        [Route("optional-activity")]
        public async Task<IActionResult> CreateOptionalActivityLog([FromBody] AttendanceLogRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var result = await _attendanceLogService.OptionalActivityAttendanceAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.AttendanceLogId }, result);
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
    }
}
