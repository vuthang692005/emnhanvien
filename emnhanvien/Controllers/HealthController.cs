using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EmNhanVien.Controllers
{
    [Route("health")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Healthy");
        }
    }
}
