using Finance.Tracking.Models;
using Finance.Tracking.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Finance.Tracking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        /// <summary>
        /// Execute a buy or sell order for an account
        /// </summary>
        [HttpPost("execute")]
        public async Task<IActionResult> ExecuteOrder([FromBody] Order order)
        {
            if (order == null)
                return BadRequest("Order cannot be null.");

            try
            {
                await _transactionService.ExecuteOrder(order);
                return Ok(new { message = "Order executed successfully." });
            }
            catch (NullReferenceException ex)
            {
                // Account not found
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // e.g., selling more than owned
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // General error
                return StatusCode(500, new { message = "An error occurred.", detail = ex.Message });
            }
        }
    }
}
