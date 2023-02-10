using Jiro.Core;
using Jiro.Core.Base;
using Microsoft.AspNetCore.Mvc;

namespace Jiro.Api.Controllers;

public class JiroController : BaseController
{
    private readonly ICommandHandlerService _commandHandlerService;

    public JiroController(ICommandHandlerService commandHandlerService)
    {
        _commandHandlerService = commandHandlerService;
    }

    [HttpPost]
    public async Task<IActionResult> PushCommand([FromBody] JiroPromptDTO query)
    {
        var result = await _commandHandlerService.ExecuteCommandAsync(query.Prompt);

        return ApiResponseCreator.Data(result);
    }

    [HttpGet("1")]
    public IActionResult Error1()
    {
        throw new HandledException("Error1");
        return Ok();
    }

    [HttpGet("2")]
    public IActionResult Error2()
    {
        throw new HandledExceptionList(new string[] { "Error1", "Error2" });
        return Ok();
    }

    [HttpGet("3")]
    public IActionResult Error3()
    {
        throw new Exception("Error1");
        return Ok();
    }
}