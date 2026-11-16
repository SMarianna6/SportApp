using Microsoft.AspNetCore.Mvc;
using SportLab.Data;
using System.Linq;

namespace SportLab.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApiTestController : ControllerBase
    {
        private readonly SportLabContext _context;

        public ApiTestController(SportLabContext context)
        {
            _context = context;
        }

        [HttpGet("tariffs")]
        public IActionResult GetTariffs()
        {
            var tariffs = _context.Plans.ToList();
            return Ok(tariffs);
        }
    }
}