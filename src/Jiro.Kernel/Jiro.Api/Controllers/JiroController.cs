using Jiro.Core.Base.Models;
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
    [ProducesResponseType(typeof(ApiResponse<CommandResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> PushCommand([FromBody] JiroPromptDTO query)
    {
        var result = await _commandHandlerService.ExecuteCommandAsync(query.Prompt);

        return ApiResponseCreator.Data(result);
    }

    [HttpGet("completion")]
    public async Task<IActionResult> GetCompletion(string query)
    {
        // var result = await _commandHandlerService.GetCompletionAsync(query);

        return ApiResponseCreator.ValueType(true);
    }
}