using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigurationsController : ControllerBase
    {
        [HttpGet("room-types")]
        public IActionResult GetRoomTypes() => Ok();

        [HttpPost("room-types")]
        public IActionResult CreateRoomType() => Ok();

        [HttpGet("rooms")]
        public IActionResult GetRooms() => Ok();

        [HttpPost("rooms")]
        public IActionResult CreateRoom() => Ok();

        [HttpPut("rooms/{id}/status")]
        public IActionResult UpdateRoomStatus(int id) => Ok();

        [HttpGet("services")]
        public IActionResult GetServices() => Ok();

        [HttpPost("services")]
        public IActionResult CreateService() => Ok();

        [HttpGet("employees")]
        public IActionResult GetEmployees() => Ok();

        [HttpPost("employees")]
        public IActionResult CreateEmployee() => Ok();
    }
}
