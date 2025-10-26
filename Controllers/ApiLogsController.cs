using ApiLogDemo.Models;
using ApiLogDemo.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiLogDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApiLogsController : ControllerBase
    {
        private readonly ApiLogService _logService;

        public ApiLogsController(ApiLogService logService)
        {
            _logService = logService;
        }

        [HttpGet]
        public IActionResult GetAllLogs([FromQuery] ApiMethod? method = null)
        {
            var logs = _logService.GetLogs(method?.ToString());
            return Ok(logs);
        }
    }
}
