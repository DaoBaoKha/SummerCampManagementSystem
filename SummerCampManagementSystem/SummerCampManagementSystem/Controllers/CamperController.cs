using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Camper;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;

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
        {
            var campers = await _camperService.GetAllCampersAsync();
            return Ok(campers);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var camper = await _camperService.GetCamperByIdAsync(id);
            if (camper == null)
                return NotFound(new { message = $"Camper with id {id} not found." });

            return Ok(camper);
        }

        [HttpGet("camp/{campId}")]
        public async Task<IActionResult> GetByCampId(int campId)
        {
            try
            {
                var camper = await _camperService.GetCampersByCampId(campId);
                return Ok(camper);

            }
            catch (Exception ex)
            {
                return NotFound(new { message = $"Camp with id {campId} not found." });
            }
        }

        [HttpGet("{camperId}/guardians")]
        public async Task<IActionResult> GetGuardiansByCamperId(int camperId)
        {
            try
            {
                var guardians = await _camperService.GetGuardiansByCamperId(camperId);
                return Ok(guardians);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("activityScheduleId{id}")]
        public async Task<IActionResult> GetCampersByOptionalActivitySchedule(int id)
        {
            try
            {
                var campers = await _camperService.GetCampersByOptionalActivitySChedule(id);
                return Ok(campers);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(CamperRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var created = await _camperService.CreateCamperAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.CamperId }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });

            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, CamperRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var updated = await _camperService.UpdateCamperAsync(id, dto);
            if (!updated)
                return NotFound(new { message = $"Camper with id {id} not found." });

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _camperService.DeleteCamperAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Camper with id {id} not found." });

            return NoContent();
        }
    }
}
