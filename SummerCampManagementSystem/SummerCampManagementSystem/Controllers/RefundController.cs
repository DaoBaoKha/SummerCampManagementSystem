using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Refund;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/refund")]
    [ApiController]
    [Authorize] 
    public class RefundController : ControllerBase
    {
        private readonly IRefundService _refundService;

        public RefundController(IRefundService refundService)
        {
            _refundService = refundService;
        }

        /// <summary>
        /// Calculate refund amount before requesting
        /// </summary>
        [HttpGet("calculate/{registrationId}")]
        public async Task<IActionResult> CalculateRefund(int registrationId)
        {
            var result = await _refundService.CalculateRefundAsync(registrationId);
            return Ok(result);
        }


        [HttpPost("request-cancel")]
        public async Task<IActionResult> RequestCancel([FromBody] CancelRequestDto requestDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _refundService.RequestCancelAsync(requestDto);

            result.RequestDate = result.RequestDate.ToVietnamTime();

            return Ok(result);
        }
    }
}