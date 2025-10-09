using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Requests.Registration;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/registration")]
    [ApiController]
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

        [HttpPost]
        // THAY ĐỔI 1: Dùng CreateRegistrationRequestDto
        public async Task<IActionResult> CreateRegistration([FromBody] RegistrationRequestDto registration)
        {
            if (registration == null || !registration.CamperIds.Any())
            {
                return BadRequest("Request body is empty or CamperIds list cannot be empty.");
            }
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var newRegistration = await _registrationService.CreateRegistrationAsync(registration);
            return CreatedAtAction(nameof(GetRegistrationById),
                new { id = newRegistration.registrationId }, newRegistration);
        }

        [HttpPut("{id}")]
        // THAY ĐỔI 2: Dùng UpdateRegistrationRequestDto
        public async Task<IActionResult> UpdateRegistration(int id, [FromBody] RegistrationRequestDto registration)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updatedRegistration = await _registrationService.UpdateRegistrationAsync(id, registration);
            if (updatedRegistration == null)
            {
                return NotFound(new { message = $"Registration with ID {id} not found" });
            }
            return Ok(updatedRegistration);
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