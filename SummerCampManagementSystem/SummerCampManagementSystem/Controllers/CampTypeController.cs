using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.CampType;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/camptype")]
    [ApiController]
    public class CampTypeController : ControllerBase
    {
        private readonly ICampTypeService _campTypeService;

        public CampTypeController(ICampTypeService campTypeService)
        {
            _campTypeService = campTypeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCampTypes()
        {
            var campTypes = await _campTypeService.GetAllCampTypesAsync();
            return Ok(campTypes);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCampTypeById(int id)
        {
            var campType = await _campTypeService.GetCampTypeByIdAsync(id);
            if (campType == null)
            {
                return NotFound(new { message = "Camp type not found" });
            }
            return Ok(campType);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCampType([FromBody] CampTypeRequestDto campType)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var createdCampType = await _campTypeService.AddCampTypeAsync(campType);
            return CreatedAtAction(nameof(GetCampTypeById), new { id = createdCampType.CampTypeId }, createdCampType);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCampType(int id, [FromBody] CampTypeRequestDto campType)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var updatedCampType = await _campTypeService.UpdateCampTypeAsync(id, campType);
            if (updatedCampType == null)
            {
                return NotFound(new { message = "Camp type not found" });
            }
            return Ok(updatedCampType);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCampType(int id)
        {
            var result = await _campTypeService.DeleteCampTypeAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Camp type not found" });
            }
            return Ok(id);
        }
    }
}
