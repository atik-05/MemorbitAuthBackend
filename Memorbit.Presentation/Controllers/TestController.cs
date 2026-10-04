using Microsoft.AspNetCore.Mvc;

namespace Memorbit.Presentation.Controllers
{
    [ApiController]
    [Route("api/test")]
    public class TestController : ControllerBase
    {

        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Welcome to your personal space.");
        }
    }
}
