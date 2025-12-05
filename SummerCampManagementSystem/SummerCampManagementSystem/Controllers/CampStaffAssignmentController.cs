using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.CampStaffAssignment;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin, Manager")]
    [Route("api/campstaffassignment")]

    public class CampStaffAssignmentsController : ControllerBase
    {
        private readonly ICampStaffAssignmentService _assignmentService;

        public CampStaffAssignmentsController(ICampStaffAssignmentService assignmentService)
        {
            _assignmentService = assignmentService;
        }

        [HttpPost]
        public async Task<IActionResult> AssignStaff([FromBody] CampStaffAssignmentRequestDto requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var newAssignment = await _assignmentService.AssignStaffToCampAsync(requestDto);

                return CreatedAtAction(
                    nameof(GetAssignmentById),
                    new { id = newAssignment.CampStaffAssignmentId },
                    newAssignment
                );
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin, Manager, Staff")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAssignmentById(int id)
        {
            var assignment = await _assignmentService.GetAssignmentByIdAsync(id);

            if (assignment == null)
            {
                return NotFound(new { message = $"Assignment with ID {id} not found." });
            }

            return Ok(assignment);
        }

        [Authorize(Roles = "Admin, Manager, Staff")]
        [HttpGet("camp/{campId}")]
        public async Task<IActionResult> GetAssignmentsByCamp(int campId)
        {
            var assignments = await _assignmentService.GetAssignmentsByCampIdAsync(campId);
            return Ok(assignments); 
        }

        [Authorize(Roles = "Admin, Manager, Staff")]
        [HttpGet("staff/{staffId}")]
        public async Task<IActionResult> GetAssignmentsByStaff(int staffId)
        {
            var assignments = await _assignmentService.GetAssignmentsByStaffIdAsync(staffId);
            return Ok(assignments); 
        }

        [Authorize(Roles = "Admin, Manager, Staff")]
        [HttpGet("availableStaff/{campId}")]
        public async Task<IActionResult> GetAvailableStaffByCampId(int campId)
        {
            try
            {
                var availableStaffs = await _assignmentService.GetAvailableStaffManagerByCampIdAsync(campId);
                return Ok(availableStaffs);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAssignment(int id)
        {
            try
            {
                await _assignmentService.DeleteAssignmentAsync(id);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
