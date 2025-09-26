using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VoiceInputAssistant.WebApi.Controllers;

/// <summary>
/// Test controller for verifying API functionality
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;

    public TestController(ILogger<TestController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public ActionResult<object> Health()
    {
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Service = "Voice Input Assistant API"
        });
    }

    /// <summary>
    /// Echo endpoint for testing
    /// </summary>
    [HttpPost("echo")]
    [AllowAnonymous]
    public ActionResult<object> Echo([FromBody] object message)
    {
        return Ok(new
        {
            Echo = message,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Protected endpoint requiring authentication
    /// </summary>
    [HttpGet("protected")]
    public ActionResult<object> Protected()
    {
        var userId = User.FindFirst("userId")?.Value;
        var email = User.FindFirst("email")?.Value;

        return Ok(new
        {
            Message = "You are authenticated!",
            UserId = userId,
            Email = email,
            Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
        });
    }
}