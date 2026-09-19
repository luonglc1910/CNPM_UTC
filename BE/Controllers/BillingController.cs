using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BillingController : ControllerBase
    {
        [HttpGet("folios/{stayId}")]
        public IActionResult GetFolio(int stayId) => Ok();

        [HttpPost("folios/{stayId}/add-charge")]
        public IActionResult AddCharge(int stayId) => Ok();

        [HttpPost("payments")]
        public IActionResult ProcessPayment() => Ok();

        [HttpPost("invoices/generate/{stayId}")]
        public IActionResult GenerateInvoice(int stayId) => Ok();

        [HttpGet("reports/revenue")]
        public IActionResult GetRevenueReport() => Ok();

        [HttpGet("reports/occupancy")]
        public IActionResult GetOccupancyReport() => Ok();
    }
}
