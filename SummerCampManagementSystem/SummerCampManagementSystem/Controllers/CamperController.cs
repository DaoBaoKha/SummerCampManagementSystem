using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Camper;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.BLL.Services;
using SummerCampManagementSystem.DAL.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CamperController : ControllerBase
    {
        private readonly ICamperService _camperService;
        private readonly IUserContextService _userContextService;

        public CamperController(ICamperService camperService, IUserContextService userContextService)
        {
            _camperService = camperService;
            _userContextService = userContextService;
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

        [Authorize(Roles = "User")]
        [HttpGet("my-campers")]
        public async Task<IActionResult> GetMyCampers()
        {
            var userId = _userContextService.GetCurrentUserId();
            var campers = await _camperService.GetByParentIdAsync(userId.Value);
            return Ok(campers);
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

        [HttpGet("optionalActivities/{optionalActivityId}/campers")]
        public async Task<IActionResult> GetCampersByOptionalActivitySchedule(int optionalActivityId)
        {
            try
            {
                var campers = await _camperService.GetCampersByOptionalActivitySChedule(optionalActivityId);
                return Ok(campers);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Staff")]
        [HttpGet("coreActivities/{coreActivityId}/campers")]
        public async Task<IActionResult> GetCampersByCoreActivity(int coreActivityId)
        {
            try
            {
                var staffId = _userContextService.GetCurrentUserId();
                var campers = await _camperService.GetCampersByCoreActivityIdAsync(coreActivityId, staffId.Value);
                return Ok(campers);
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
                return StatusCode(500, new { message = "Unexpected error", detail = ex.Message });
            }
        }

        [Authorize(Roles = "User")]             
        [HttpPost]
        public async Task<IActionResult> Create(CamperRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = _userContextService.GetCurrentUserId();
                var created = await _camperService.CreateCamperAsync(dto, userId.Value);
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
