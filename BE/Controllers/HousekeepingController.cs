using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HousekeepingController : ControllerBase
    {
        [HttpGet("rooms/status")]
        public IActionResult GetRoomsStatus() => Ok();

        [HttpPut("rooms/{roomId}/status")]
        public IActionResult UpdateRoomStatus(int roomId) => Ok();

        [HttpPost("requests")]
        public IActionResult CreateServiceRequest() => Ok();

        [HttpGet("requests/pending")]
        public IActionResult GetPendingRequests() => Ok();

        [HttpPut("requests/{id}/complete")]
        public IActionResult CompleteRequest(int id) => Ok();

        [HttpPost("rooms/{roomId}/minibar-usage")]
        public IActionResult RecordMinibarUsage(int roomId) => Ok();
    }
}
