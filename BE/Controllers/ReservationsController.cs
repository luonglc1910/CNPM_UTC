using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationsController : ControllerBase
    {
        [HttpGet("availability")]
        public IActionResult CheckAvailability() => Ok();

        [HttpPost("book")]
        public IActionResult BookRoom() => Ok();

        [HttpGet("{id}")]
        public IActionResult GetReservation(int id) => Ok();

        [HttpPut("{id}")]
        public IActionResult UpdateReservation(int id) => Ok();

        [HttpPut("{id}/cancel")]
        public IActionResult CancelReservation(int id) => Ok();

        [HttpGet("upcoming")]
        public IActionResult GetUpcomingReservations() => Ok();
    }
}
