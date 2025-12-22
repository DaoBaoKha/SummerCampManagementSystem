using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.Refund;
using SummerCampManagementSystem.BLL.Helpers;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.Core.Enums;

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

        /// <summary>
        /// Get all refund requests with optional filtering
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet("requests")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> GetAllRefundRequests([FromQuery] RefundRequestFilterDto filter)
        {
            var results = await _refundService.GetAllRefundRequestsAsync(filter);

            return Ok(results);
        }

        /// <summary>
        /// Get refund requests for a specific camp with optional filtering
        /// </summary>
        [HttpGet("camp/{campId}/requests")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> GetRefundRequestsByCamp(int campId, [FromQuery] RefundRequestFilterDto filter)
        {
            var results = await _refundService.GetRefundRequestsByCampAsync(campId, filter);

            return Ok(results);
        }

        /// <summary>
        /// Get current user refund requests
        /// </summary>
        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRefundRequests([FromQuery] RefundRequestFilterDto filter)
        {
            var results = await _refundService.GetMyRefundRequestsAsync(filter);

            return Ok(results);
        }


        [HttpPost("approve")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> ApproveRefund([FromForm] ApproveRefundDto dto)
        {
            if (!ModelState.IsValid) 
                return BadRequest(ModelState);

            var result = await _refundService.ApproveRefundAsync(dto);

            result.RequestDate = result.RequestDate.ToVietnamTime();

            return Ok(result);
        }

        [HttpPost("reject")]
        [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> RejectRefund([FromBody] RejectRefundDto dto)
        {
            if (!ModelState.IsValid) 
                return BadRequest(ModelState);

            var result = await _refundService.RejectRefundAsync(dto);

            result.RequestDate = result.RequestDate.ToVietnamTime();

            return Ok(result);
        }
    }
}