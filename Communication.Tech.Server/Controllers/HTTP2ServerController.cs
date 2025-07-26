using Communication.Tech.Server.Models;
using Microsoft.AspNetCore.Mvc;

namespace Communication.Tech.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class HTTP2ServerController : ControllerBase
{
    [HttpPost(Name = "HelloMessageFromHTTP2Server")]
    public IActionResult HelloMessage(ApiRequest request)
    {
        return Ok(new { request.Message });
    }
}