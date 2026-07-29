using Microsoft.AspNetCore.Mvc;

namespace Sofistike.Api.Controllers;

[ApiController]
[Route("api/v1/system")]
public sealed class SystemController(IHostEnvironment environment) : ControllerBase
{
    [HttpGet("health")]
    [ProducesResponseType<SystemHealthResponse>(StatusCodes.Status200OK)]
    public ActionResult<SystemHealthResponse> GetHealth()
    {
        return Ok(
            new SystemHealthResponse(
                "ok",
                "Sofistike.Api",
                environment.EnvironmentName,
                DateTimeOffset.UtcNow
            )
        );
    }
}

public sealed record SystemHealthResponse(
    string Status,
    string Service,
    string Environment,
    DateTimeOffset Timestamp
);
