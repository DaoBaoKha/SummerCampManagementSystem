using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Driver;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/driver")]
    [ApiController]
    public class DriverController : ControllerBase
    {
        private readonly IDriverService _driverService;
        private readonly ILogger<DriverController> _logger;

        public DriverController(IDriverService driverService, ILogger<DriverController> logger)
        {
            _driverService = driverService;
            _logger = logger;
        }

        /// <summary>
        /// Get Available Driver
        /// </summary>
        /// <param name="date">Check Schedule Date (2025-12-31)</param>
        /// <param name="startTime">Start Time (08:00:00)</param>
        /// <param name="endTime">End Time (12:00:00)</param>
        [HttpGet("available")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<ActionResult<IEnumerable<DriverResponseDto>>> GetAvailableDrivers( [FromQuery] DateOnly? date, [FromQuery] TimeOnly? startTime, [FromQuery] TimeOnly? endTime)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var drivers = await _driverService.GetAvailableDriversAsync(date, startTime, endTime);

            return Ok(drivers);
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

                _logger.LogInformation("Driver registered successfully. Attempting to create location URI.");

                return CreatedAtAction(nameof(GetDriverByUserId), new { userId = userResponse.UserId }, userResponse);
            }
            // email already exists
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Registration failed: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message }); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CRITICAL ERROR: Failed to register driver or create CreatedAtAction response. Host/URI issue suspected. Host: {Host}, Scheme: {Scheme}", 
                                 HttpContext.Request.Host.Value, HttpContext.Request.Scheme);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Đăng ký thất bại do lỗi hệ thống.", detail = ex.Message });
            }
        }

        [HttpPut("upload-photo")]
        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> UploadDriverLicensePhoto([FromForm] DriverLicensePhotoUploadDto model)
        {
            if (!ModelState.IsValid)
            {
                var errorMessage = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault()
                                   ?? "Dữ liệu tải lên không hợp lệ.";
                return BadRequest(new { message = errorMessage });
            }
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var photoUrl = await _driverService.UpdateDriverLicensePhotoAsync(model.LicensePhoto);
                return Ok(new { LicensePhotoUrl = photoUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Tải ảnh giấy phép lái xe thất bại do lỗi hệ thống.", detail = ex.Message });
            }
        }

        [HttpPost("upload-photo-by-token")]
        public async Task<IActionResult> UploadDriverLicensePhotoByToken([FromForm] DriverLicenseUploadByTokenDto model)
        {
            if (!ModelState.IsValid)
            {
                var errorMessage = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault()
                                       ?? "Dữ liệu tải lên không hợp lệ.";
                return BadRequest(new { message = errorMessage });
            }

            try
            {
                var photoUrl = await _driverService.UpdateDriverLicensePhotoByTokenAsync(model.UploadToken, model.LicensePhoto);

                return Ok(new { message = "Upload ảnh giấy phép lái xe thành công. Đang chờ phê duyệt.", LicensePhotoUrl = photoUrl });
            }
            catch (KeyNotFoundException ex)
            {
                // token invalid or not existed
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // token expired or used
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Tải ảnh giấy phép lái xe thất bại do lỗi hệ thống.", detail = ex.Message });
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

        [HttpGet("status")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> GetDriversByStatus([FromQuery] string status)
        {
            try
            {
                var drivers = await _driverService.GetDriverByStatusAsync(status);
                return Ok(drivers);
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

        [HttpPut("{driverId}/status")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> UpdateDriverStatus(int driverId, [FromQuery] DriverStatusUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                var errorMessage = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault()
                                           ?? "Dữ liệu trạng thái không hợp lệ.";
                return BadRequest(new { message = errorMessage });
            }

            try
            {
                var updatedDriver = await _driverService.UpdateDriverStatusAsync(driverId, updateDto);

                return Ok(new { message = $"Trạng thái Driver {driverId} đã được cập nhật thành {updatedDriver.Role}.", driver = updatedDriver });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex) // error status update
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex) // flow error
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống nội bộ." });
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