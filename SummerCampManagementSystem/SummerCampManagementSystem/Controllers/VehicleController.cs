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
            Console.WriteLine("🔄 [VehicleController] GET /api/vehicle");
            var vehicles = await _vehicleService.GetAllVehicles();
            Console.WriteLine($"✅ [VehicleController] Retrieved {vehicles?.Count()} vehicles");

            // Log first vehicle for debugging
            var firstVehicle = vehicles?.FirstOrDefault();
            if (firstVehicle != null)
            {
                Console.WriteLine($"📋 [VehicleController] Sample vehicle data:");
                Console.WriteLine($"   - vehicleId: {firstVehicle.vehicleId}");
                Console.WriteLine($"   - vehicleName: {firstVehicle.vehicleName}");
                Console.WriteLine($"   - vehicleType: {firstVehicle.vehicleType}");
                Console.WriteLine($"   - vehicleTypeNavigation: {firstVehicle.vehicleTypeNavigation?.name}");
            }

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
            Console.WriteLine($"🔄 [VehicleController] PUT /api/vehicle/{id}");
            Console.WriteLine($"📥 [VehicleController] Received vehicle data:");
            Console.WriteLine($"   - vehicleId: {vehicle.vehicleId}");
            Console.WriteLine($"   - vehicleName: {vehicle.vehicleName}");
            Console.WriteLine($"   - vehicleNumber: {vehicle.vehicleNumber}");
            Console.WriteLine($"   - capacity: {vehicle.capacity}");
            Console.WriteLine($"   - status: {vehicle.status}");
            Console.WriteLine($"   - vehicleType: {vehicle.vehicleType}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine($"❌ [VehicleController] ModelState is invalid");
                return BadRequest(new { message = "Input data is invalid" });
            }

            var existing = await _vehicleService.GetVehicleById(id);
            if (existing == null)
            {
                Console.WriteLine($"❌ [VehicleController] Vehicle with id {id} not found");
                return NotFound(new { message = "Vehicle not found" });
            }

            Console.WriteLine($"📋 [VehicleController] Existing vehicle:");
            Console.WriteLine($"   - vehicleType (before): {existing.vehicleType}");

            await _vehicleService.UpdateVehicleAsync(vehicle);

            Console.WriteLine($"✅ [VehicleController] Vehicle updated successfully");

            // Get the updated vehicle to verify
            var updated = await _vehicleService.GetVehicleById(id);
            Console.WriteLine($"📋 [VehicleController] Updated vehicle:");
            Console.WriteLine($"   - vehicleType (after): {updated?.vehicleType}");
            Console.WriteLine($"   - vehicleTypeNavigation: {updated?.vehicleTypeNavigation?.name}");

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
