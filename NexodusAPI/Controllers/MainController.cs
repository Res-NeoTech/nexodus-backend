using Microsoft.AspNetCore.Mvc;
using NexodusAPI.Attributes;

namespace NexodusAPI.Controllers
{
    [Route("/")]
    [ApiController]
    public class MainController : ControllerBase
    {
        [HttpGet]
        [BypassProxyValidation]
        public IActionResult HeartBeatCheck()
        {
            return Ok("Heartbeat, my heartbeat.");
        }
    }
}