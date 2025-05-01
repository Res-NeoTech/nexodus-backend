using Microsoft.AspNetCore.Mvc;

namespace NexodusAPI.Controllers
{
    [Route("/")]
    [ApiController]
    public class MainController : ControllerBase
    {
        [HttpGet]
        public IActionResult HeartBeatCheck()
        {
            return Ok("Heartbeat, my heartbeat.");
        }
    }
}