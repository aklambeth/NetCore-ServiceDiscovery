using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TestWebApp.Configuration;

namespace TestWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly DemoAppSettings _config;

        public TestController(ILogger<TestController> logger, IOptionsMonitor<DemoAppSettings> config)
        {
            _logger = logger;
            _config = config.CurrentValue;
        }

        // GET: api/Default
        [HttpGet]
        public string Test()
        {
            _logger.LogInformation("Request Received -> OK");
            return $"{_config.Environment}: OK";
        }
    }
}