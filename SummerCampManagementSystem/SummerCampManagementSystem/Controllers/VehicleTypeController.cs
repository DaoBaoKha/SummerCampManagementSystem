    using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.VehicleType;
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
        {
            var types = await _vehicleTypeService.GetAllVehicleTypesAsync();
            return Ok(types);
        }

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
        {
            var types = await _vehicleTypeService.GetActiveVehicleAsync();
            return Ok(types);
        }

        // POST api/<VehicleTypeController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] VehicleTypeRequestDto type)
        {
           if(!ModelState.IsValid)
           {
                return BadRequest(ModelState);
           }
            var createdType = await _vehicleTypeService.CreateVehicleTypeAsync(type);
            return CreatedAtAction(nameof(Get), new { id = createdType.vehicleTypeId }, createdType);
        }

        // PUT api/<VehicleTypeController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] VehicleTypeUpdateDto vehicleType)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _vehicleTypeService.UpdateVehicleTypeAsync(id, vehicleType);
            return result ? NoContent() : NotFound(new {message = "Vehicle type not found"});
        }

        // DELETE api/<VehicleTypeController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var res = await _vehicleTypeService.DeleteVehicleTypeAsync(id);
            return res ? NoContent() : NotFound(new { message = "Vehicle type not found" });
        }
    }
}
