using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Driver;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/drivers")]
    [ApiController]
    public class DriverController : ControllerBase
    {
        private readonly IDriverService _driverService;

        public DriverController(IDriverService driverService)
        {
            _driverService = driverService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterDriver([FromBody] DriverRegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                var errorMessage = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault()
                                   ?? "Dữ liệu đăng ký không hợp lệ.";
                return BadRequest(new { message = errorMessage });
            }

            try
            {
                var userResponse = await _driverService.RegisterDriverAsync(model);

                return CreatedAtAction(nameof(GetDriverByUserId), new { userId = userResponse.UserId }, userResponse);
            }
            // email already exists
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message }); 
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Đăng ký thất bại do lỗi hệ thống.", detail = ex.Message });
            }
        }


        [HttpGet]
        [Authorize(Roles = "Admin, Manager")] 
        public async Task<IActionResult> GetAllDrivers()
        {
            var drivers = await _driverService.GetAllDriversAsync();
            return Ok(drivers); 
        }

        [HttpGet("user/{userId}")]
        [Authorize] 
        public async Task<IActionResult> GetDriverByUserId(int userId)
        {
            try
            {
                var driver = await _driverService.GetDriverByUserIdAsync(userId);
                return Ok(driver);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message }); 
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống nội bộ.", detail = ex.Message });
            }
        }

  

        [HttpPut("{driverId}")]
        [Authorize]
        public async Task<IActionResult> UpdateDriver(int driverId, [FromBody] DriverRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Dữ liệu cập nhật không hợp lệ." });
            }
            try
            {
                var updatedDriver = await _driverService.UpdateDriverAsync(driverId, model);
                return Ok(updatedDriver);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống nội bộ.", detail = ex.Message });
            }
        }


        [HttpDelete("{driverId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDriver(int driverId)
        {
            try
            {
                var result = await _driverService.DeleteDriverAsync(driverId);

                return NoContent(); 
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống nội bộ.", detail = ex.Message });
            }
        }
    }
}