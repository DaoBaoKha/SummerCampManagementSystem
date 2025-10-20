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

        [HttpPost]
        //use create registration dto
        public async Task<IActionResult> CreateRegistration([FromBody] CreateRegistrationRequestDto registration)
        {
            if (registration == null || !registration.CamperIds.Any())
            {
                return BadRequest("Request body is empty or CamperIds list cannot be empty.");
            }
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var reponse = await _registrationService.CreateRegistrationAsync(registration);

            return CreatedAtAction(nameof(GetRegistrationById),
                new { id = reponse.registrationId }, reponse);
        }

        [HttpPost("{id}/payment-link")]
        public async Task<IActionResult> GeneratePaymentLink(int id)
        {
            try
            {
                var response = await _registrationService.GeneratePaymentLinkAsync(id);
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRegistration(int id, [FromBody] UpdateRegistrationRequestDto registration)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updatedRegistration = await _registrationService.UpdateRegistrationAsync(id, registration);
            if (updatedRegistration == null)
            {
                return NotFound(new { message = $"Registration with ID {id} not found" });
            }
            return Ok(updatedRegistration);
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