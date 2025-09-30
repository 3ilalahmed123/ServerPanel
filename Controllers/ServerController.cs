using Microsoft.AspNetCore.Mvc;
using site.Services;

[ApiController]
[Route("api/[controller]")]
public class ServerController : ControllerBase
{
    private readonly CssServerService _cssServerService;

    public ServerController(CssServerService cssServerService)
    {
        _cssServerService = cssServerService;
    }

    [HttpGet("is-started")]
    public async Task<ActionResult<bool>> IsServerStarted()
    {
        bool isStarted = await _cssServerService.IsServerStartedAsync();
        return Ok(isStarted);
    }
}