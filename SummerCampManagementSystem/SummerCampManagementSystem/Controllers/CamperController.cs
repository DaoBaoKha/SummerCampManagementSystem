using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Requests.Camper;
using SummerCampManagementSystem.BLL.Interfaces;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CamperController : ControllerBase
    {
        private readonly ICamperService _camperService;

        public CamperController(ICamperService camperService)
        {
            _camperService = camperService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
      => Ok(await _camperService.GetAllCampersAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var camper = await _camperService.GetCamperByIdAsync(id);
            return camper == null ? NotFound() : Ok(camper);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CamperCreateDto dto)
        {
            var created = await _camperService.CreateCamperAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.CamperId }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, CamperUpdateDto dto)
        {
            if (id != dto.CamperId) return BadRequest();
            var updated = await _camperService.UpdateCamperAsync(dto);
            return updated ? NoContent() : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _camperService.DeleteCamperAsync(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}
