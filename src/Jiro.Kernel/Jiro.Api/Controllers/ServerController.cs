using Jiro.Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jiro.Api.Controllers;

[Authorize(Roles = Roles.SERVER)]
public class ServerController : BaseController
{
    private readonly IJiroInstanceService _jiroInstanceService;
    private readonly ILogger _logger;
    public ServerController(IJiroInstanceService jiroInstanceService, ILogger<ServerController> logger)
    {
        _jiroInstanceService = jiroInstanceService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiSuccessResponse<InstanceConfigDTO>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> GetServerConfig()
    {
        var config = await _jiroInstanceService.GetConfigAsync();

        return Ok(new ApiSuccessResponse<InstanceConfigDTO>(config));
    }

    [HttpPut]
    [ProducesResponseType(typeof(ApiSuccessResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> UpdateServiceConfig([FromBody] InstanceConfigDTO config)
    {
        await _jiroInstanceService.ConfigureAsync(config);

        _ = Task.Run(async () =>
        {
            _logger.LogInformation("Restarting Jiro in 10 seconds...");
            await Task.Delay(10000);

            Program.Restart();
        });

        return Ok(new ApiSuccessResponse<bool>(true));
    }

    [HttpGet("restart")]
    [ProducesResponseType(typeof(ApiSuccessResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public void Restart()
    {
        Program.Restart();
    }
}