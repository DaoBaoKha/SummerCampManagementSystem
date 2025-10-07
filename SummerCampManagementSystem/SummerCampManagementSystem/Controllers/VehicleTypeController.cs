    using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/vehicletype")]
    [ApiController]
    public class VehicleTypeController : ControllerBase
    {
        private readonly IVehicleTypeService _vehicleTypeService;
        public VehicleTypeController(IVehicleTypeService vehicleTypeService)
        {
            _vehicleTypeService = vehicleTypeService;
        }
        // GET: api/<VehicleTypeController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        => Ok(await _vehicleTypeService.GetAllVehicleTypesAsync());

        // GET api/<VehicleTypeController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var type = await _vehicleTypeService.GetVehicleTypeByIdAsync(id);
            if (type == null)
            {
                return NotFound(new { message = $"Vehicle type with id {id} not found" });
            }
            return Ok(type);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
      => Ok(await _vehicleTypeService.GetActiveVehicleAsync());

        // POST api/<VehicleTypeController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] VehicleType type)
        {
            if (ModelState.IsValid)
            {
                await _vehicleTypeService.CreateVehicleTypeAsync(type);
                return Ok(new { message = "Vehicle type added successfully" });
            }
            else
            {
                return BadRequest(ModelState);

            }
        }

        // PUT api/<VehicleTypeController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] VehicleType vehicleType)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new {message= "Input data is invalid" });
            }

            var existing = await _vehicleTypeService.GetVehicleTypeByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = "Vehicle type not found" });
            }

            await _vehicleTypeService.UpdateVehicleTypeAsync(vehicleType);
            return Ok(new { message = "Vehicle type updated successfully" });
        }

        // DELETE api/<VehicleTypeController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _vehicleTypeService.GetVehicleTypeByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = "Vehicle type not found" });
            }
            await _vehicleTypeService.DeleteVehicleTypeAsync(id);
            return Ok(new { message = "Vehicle type deleted successfully" });
        }
    }
}
