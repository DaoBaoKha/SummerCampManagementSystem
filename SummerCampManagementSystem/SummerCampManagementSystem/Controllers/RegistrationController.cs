using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Registration;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/registration")]
    [ApiController]
    [Authorize]
    public class RegistrationController : ControllerBase
    {
        private readonly IRegistrationService _registrationService;

        public RegistrationController(IRegistrationService registrationService)
        {
            _registrationService = registrationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRegistrations()
        {
            var registrations = await _registrationService.GetAllRegistrationsAsync();
            return Ok(registrations);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRegistrationById(int id)
        {
            var registration = await _registrationService.GetRegistrationByIdAsync(id);
            if (registration == null) return NotFound();
            return Ok(registration);
        }

        [HttpGet("status")] 
        public async Task<IActionResult> GetRegistrationsByStatus([FromQuery] RegistrationStatus? status)
        {
            var registrations = await _registrationService.GetRegistrationByStatusAsync(status);
            return Ok(registrations);
        }

        [HttpGet("camp/{campId}")]
        public async Task<IActionResult> GetRegistrationsByCampId(int campId)
        {
            try
            {
                var registrations = await _registrationService.GetRegistrationByCampIdAsync(campId);

                return Ok(registrations);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving registrations by camp ID.", details = ex.Message });
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetUserRegistrationHistory()
        {
            try
            {
                var history = await _registrationService.GetUserRegistrationHistoryAsync();

                return Ok(history);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving user registration history.", details = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> CreateRegistration([FromBody] CreateRegistrationRequestDto registration)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var result = await _registrationService.CreateRegistrationAsync(registration);

                return CreatedAtAction(nameof(GetRegistrationById), new { id = result.registrationId }, result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống nội bộ.", detail = ex.Message });
            }
        }

        [HttpPost("{id}/payment-link")]
        public async Task<IActionResult> GeneratePaymentLink([FromRoute] int id, [FromBody] GeneratePaymentLinkRequestDto request, [FromQuery] bool isMobile = false)
        {
            try
            {
                var response = await _registrationService.GeneratePaymentLinkAsync(id, request, isMobile);
                return Ok(response);
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
                return StatusCode(500, new { message = "An internal error occurred during payment link generation: " + ex.Message });
            }
        }


        /// <summary>
        /// Update registration or resubmit for approval
        /// </summary>
        [HttpPut("{id}")]
        [Authorize] 
        public async Task<IActionResult> UpdateRegistration(int id, [FromBody] UpdateRegistrationRequestDto request)
        {
            var result = await _registrationService.UpdateRegistrationAsync(id, request);
            return Ok(result);
        }

        /// <summary>
        /// Reject registration or specific camper within the registration
        /// </summary>
        [HttpPost("reject")]
        [Authorize(Roles = "Admin,Manager")] 
        public async Task<IActionResult> RejectRegistration([FromBody] RejectRegistrationRequestDto request)
        {
            var result = await _registrationService.RejectRegistrationAsync(request);
            return Ok(result);
        }

        [HttpPut("{id}/approve")]
        public async Task<IActionResult> ApproveRegistration(int id)
        {
            try
            {
                var response = await _registrationService.ApproveRegistrationAsync(id);
                return Ok(response);
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

        /// <summary>
        /// Cancel registration
        /// </summary>
        [HttpPost("{id}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelRegistration(int id, [FromBody] CancelRegistrationRequestDto request)
        {
            var result = await _registrationService.CancelRegistrationAsync(id, request);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRegistration(int id)
        {
            var result = await _registrationService.DeleteRegistrationAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Registration not found or could not be deleted" });
            }
            return NoContent();
        }
    }
}