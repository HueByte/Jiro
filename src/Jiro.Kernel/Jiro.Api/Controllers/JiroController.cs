using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Jiro.Commands.Models;

namespace Jiro.Api.Controllers;

[Authorize(Policy = Jiro.Core.Constants.Policies.WHITE_LIST)]
public class JiroController : BaseController
{
    private readonly ICommandHandlerService _commandHandlerService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public JiroController(ICommandHandlerService commandHandlerService, IHttpContextAccessor httpContextAccessor)
    {
        _commandHandlerService = commandHandlerService;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiSuccessResponse<CommandResponse>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> PushCommand([FromBody] JiroPromptDTO query)
    {
        CommandResponse result = await _commandHandlerService.ExecuteCommandAsync(_httpContextAccessor?.HttpContext?.RequestServices!, query.Prompt);

        return Ok(new ApiSuccessResponse<CommandResponse>(result));
    }

    // [HttpGet("completion")]
    // public async Task<IActionResult> GetCompletion(string query)
    // {
    //     // var result = await _commandHandlerService.GetCompletionAsync(query);

    //     return ApiResponseCreator.ValueType(true);
    // }
}