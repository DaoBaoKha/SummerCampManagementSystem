using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.BankUser;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/bank-user")]
    [ApiController]
    [Authorize]
    public class BankUserController : ControllerBase
    {
        private readonly IBankUserService _bankUserService;

        public BankUserController(IBankUserService bankUserService)
        {
            _bankUserService = bankUserService;
        }

        /// <summary>
        /// Get my bank accounts
        /// </summary>
        /// <returns></returns>
        [HttpGet("my-accounts")]
        public async Task<IActionResult> GetMyBankAccounts()
        {
            var result = await _bankUserService.GetMyBankAccountsAsync();

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> AddBankAccount([FromBody] BankUserRequestDto requestDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _bankUserService.AddBankAccountAsync(requestDto);

            return StatusCode(StatusCodes.Status201Created, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBankAccount(int id, [FromBody] BankUserRequestDto requestDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _bankUserService.UpdateBankAccountAsync(id, requestDto);

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBankAccount(int id)
        {
            await _bankUserService.DeleteBankAccountAsync(id);

            return NoContent();
        }

        /// <summary>
        /// Set a bank account as primary
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPatch("{id}/set-primary")]
        public async Task<IActionResult> SetPrimary(int id)
        {
            await _bankUserService.SetPrimaryBankAccountAsync(id);
            return Ok(new { message = "Đã đặt tài khoản làm mặc định thành công." });
        }
    }
}