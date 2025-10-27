using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/transaction")]
    [ApiController]
    [Authorize] 
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }


        [HttpGet("user")]
        public async Task<IActionResult> GetUserTransactionHistory()
        {
            try
            {
                var history = await _transactionService.GetUserTransactionHistoryAsync();
                return Ok(history);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message); 
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving user transaction history.");
            }
        }

        [HttpGet("user/registration/{registrationId}")]
        public async Task<IActionResult> GetTransactionsByRegistrationId(int registrationId)
        {
            try
            {
                var transactions = await _transactionService.GetTransactionsByRegistrationIdAsync(registrationId);
                return Ok(transactions);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message); 
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message); 
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving transactions by registration.");
            }
        }


        // admin/staff

        [HttpGet]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllTransactions()
        {
            try
            {
                var transactions = await _transactionService.GetAllTransactionsAsync();
                return Ok(transactions);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving all transactions.");
            }
        }

        [HttpGet("{id}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTransactionById(int id)
        {
            try
            {
                var transaction = await _transactionService.GetTransactionByIdAsync(id);
                if (transaction == null)
                {
                    return NotFound($"Transaction with ID {id} not found.");
                }
                return Ok(transaction);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving transaction details.");
            }
        }

        [HttpGet("camp/{campId}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTransactionsByCampId(int campId)
        {
            try
            {
                var transactions = await _transactionService.GetTransactionsByCampIdAsync(campId);
                return Ok(transactions);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving transactions by camp.");
            }
        }
    }
}
