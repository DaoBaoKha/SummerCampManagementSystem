using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Requests.Camp;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/camp")]
    [ApiController]
    public class CampController : ControllerBase
    {
        private readonly ICampService _campService;

        public CampController(ICampService campService)
        {
            _campService = campService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCamps()
        {
            var camps = await _campService.GetAllCampsAsync();
            return Ok(camps);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCampById(int id)
        {
            var camp = await _campService.GetCampByIdAsync(id);
            if (camp == null)
            {
                return NotFound(new { message = "Camp not found" });
            }
            return Ok(camp);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCamp(int id)
        {
            var result = await _campService.DeleteCampAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Camp not found or could not be deleted" });
            }
            return NoContent();
        }

        [HttpPost]
        public async Task<IActionResult> CreateCamp([FromBody] CampRequestDto camp)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var createdCamp = await _campService.CreateCampAsync(camp);
            return CreatedAtAction(nameof(GetCampById), new { id = createdCamp.CampId }, createdCamp);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCamp(int id, [FromBody] CampRequestDto camp)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var updatedCamp = await _campService.UpdateCampAsync(id, camp);
            if (updatedCamp == null)
            {
                return NotFound(new { message = "Camp not found" });
            }
            return Ok(updatedCamp);
        }


    }
}
