using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FrontDeskController : ControllerBase
    {
        [HttpPost("checkin")]
        public IActionResult CheckIn() => Ok();

        [HttpPost("checkin/walk-in")]
        public IActionResult WalkInCheckIn() => Ok();

        [HttpPost("checkout/{stayId}")]
        public IActionResult CheckOut(int stayId) => Ok();

        [HttpGet("stays/active")]
        public IActionResult GetActiveStays() => Ok();

        [HttpPut("stays/{stayId}/change-room")]
        public IActionResult ChangeRoom(int stayId) => Ok();

        [HttpPost("stays/{stayId}/add-guest")]
        public IActionResult AddGuest(int stayId) => Ok();
    }
}
