using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> Post([FromBody] Vehicle vehicle)
        {
            if (ModelState.IsValid)
            {
                await _vehicleService.CreateVehicleAsync(vehicle);
                return Ok(new { message = "Vehicle added successfully" });
            }
            else
            {
                return BadRequest(ModelState);

            }
        }

        // PUT api/<VehicleTypeController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Vehicle vehicle)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Input data is invalid" });
            }

            var existing = await _vehicleService.GetVehicleById(id);
            if (existing == null)
            {
                return NotFound(new { message = "Vehicle not found" });
            }

            await _vehicleService.UpdateVehicleAsync(vehicle);
            return Ok(new { message = "Vehicle updated successfully" });
        }

        // DELETE api/<VehicleTypeController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _vehicleService.GetVehicleById(id);
            if (existing == null)
            {
                return NotFound(new { message = "Vehicle not found" });
            }
            await _vehicleService.DeleteVehicleAsync(id);
            return Ok(new { message = "Vehicle deleted successfully" });
        }
    }
}
