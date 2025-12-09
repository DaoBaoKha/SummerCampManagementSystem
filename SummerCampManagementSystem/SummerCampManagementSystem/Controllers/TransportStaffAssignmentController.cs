using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.TransportStaffAssignment;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/transport-staff-assignment")]
    [ApiController]
    [Authorize] 
    public class TransportStaffAssignmentController : ControllerBase
    {
        private readonly ITransportStaffAssignmentService _service;

        public TransportStaffAssignmentController(ITransportStaffAssignmentService service)
        {
            _service = service;
        }

        /// <summary>
        /// Get list of transport staff assignments based on search criteria
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin, Manager, Staff")] 
        public async Task<IActionResult> Search([FromQuery] TransportStaffAssignmentSearchDto searchDto)
        {
            var result = await _service.SearchAssignmentsAsync(searchDto);
            return Ok(result);
        }

        /// <summary>
        /// Get available staff for transport schedule
        /// </summary>
        /// <remarks>Return staff of that camp, exclude staff with conflict in comparison with other Transport/Activity/Camp.</remarks>
        [HttpGet("available-staff/{transportScheduleId}")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> GetAvailableStaff(int transportScheduleId)
        {
            var result = await _service.GetAvailableStaffForScheduleAsync(transportScheduleId);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> AssignStaff([FromBody] TransportStaffAssignmentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.AssignStaffAsync(dto);

            return CreatedAtAction(nameof(Search), new { id = result.Id }, result);
        }


        [HttpPut("{id}")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> Update(int id, [FromBody] TransportStaffAssignmentUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.UpdateAssignmentAsync(id, dto);
            return Ok(result);
        }


        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAssignmentAsync(id);
            return NoContent();
        }
    }
}