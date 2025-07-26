using Communication.Tech.Server.Models;
using Microsoft.AspNetCore.Mvc;

namespace Communication.Tech.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class HTTPServerController : ControllerBase
{
    [HttpPost(Name = "HelloMessageFromServer")]
    public IActionResult HelloMessage(ApiRequest request)
    {
        return Ok(new { request.Message });
    }
}