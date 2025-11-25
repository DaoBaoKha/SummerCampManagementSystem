using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.RegistrationOptionalActivity;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationOptionalActivityController : ControllerBase
    {
        private readonly IRegistrationOptionalActivityService _registrationOptionalActivityService;
        public RegistrationOptionalActivityController(IRegistrationOptionalActivityService registrationOptionalActivityService)
        {
            _registrationOptionalActivityService = registrationOptionalActivityService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _registrationOptionalActivityService.GetByIdAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        /// <summary>
        /// get list or search RegistrationOptionalActivities
        /// </summary>
        /// <param name="searchDto"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RegistrationOptionalActivityResponseDto>>> Get([FromQuery] RegistrationOptionalActivitySearchDto searchDto)
        {
            // check if any search criteria is provided
            if (searchDto.RegistrationId.HasValue ||
                searchDto.CamperId.HasValue ||
                searchDto.ActivityScheduleId.HasValue ||
                !string.IsNullOrWhiteSpace(searchDto.Status))
            {
                // if has criteria, call the search function
                var searchResults = await _registrationOptionalActivityService.SearchAsync(searchDto);
                return Ok(searchResults);
            }
            else
            {
                // if no criteria, return all records
                var allResults = await _registrationOptionalActivityService.GetAllAsync();
                return Ok(allResults);
            }
        }

    }
}
