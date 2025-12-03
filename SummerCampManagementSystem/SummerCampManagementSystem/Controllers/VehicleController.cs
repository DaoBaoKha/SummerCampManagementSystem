using Google.Api;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Vehicle;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.BLL.Services;
using SummerCampManagementSystem.DAL.Models;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/vehicle")]
    [ApiController]
    public class VehicleController : ControllerBase
    {
        private readonly IVehicleService _vehicleService;
        public VehicleController(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllVehicles()
        {
            var vehicles = await _vehicleService.GetAllVehicles();
            return Ok(vehicles);
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableVehicles()
        {
            var availableVehicles = await _vehicleService.GetAvailableVehicles();
            return Ok(availableVehicles);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetVehicleById(int id)
        {
            var vehicle = await _vehicleService.GetVehicleById(id);
            if (vehicle == null)
            {
                return NotFound((new { message = $"Vehicle with id {id} not found" }));
            }
            return Ok(vehicle);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] VehicleRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await _vehicleService.CreateVehicleAsync(dto);
            return CreatedAtAction(nameof(GetVehicleById), new { id = result.vehicleId }, result);
        }

        // PUT api/<VehicleTypeController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] VehicleRequestDto vehicle)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await _vehicleService.UpdateVehicleAsync(id, vehicle);
            return result ? NoContent() : NotFound();
        }

        // DELETE api/<VehicleTypeController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _vehicleService.DeleteVehicleAsync(id);
            return result ? NoContent() : NotFound();
        }
    }
}
