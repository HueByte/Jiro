using Jiro.Api.Extensions;
using Jiro.Core.Commands.Base;
using Jiro.Core.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;

namespace Jiro.Api.Controllers;

public record BasicQuery(string prompt);

public class JiroController : BaseController
{
    private readonly ICommandHandlerService _commandHandlerService;
    public JiroController(ICommandHandlerService commandHandlerService)
    {
        _commandHandlerService = commandHandlerService;
    }

    [HttpPost]
    public async Task<IActionResult> PushCommand([FromBody] BasicQuery query)
    {
        var result = await _commandHandlerService.ExecuteCommandAsync(query.prompt);
        return Ok(result);
    }
}