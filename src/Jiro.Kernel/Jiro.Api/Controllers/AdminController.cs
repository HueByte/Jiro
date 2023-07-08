using Jiro.Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Jiro.Api.Controllers;

[Authorize(Roles = Roles.ADMIN)]
public class AdminController : BaseController
{
    private readonly IUserService _userService;
    public AdminController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("assingRole")]
    [ProducesResponseType(typeof(ApiSuccessResponse<IdentityResult>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> AssingRole([FromBody] AssignRoleDTO assignRoleDTO)
    {
        var data = await _userService.AssignRoleAsync(assignRoleDTO.UserId, assignRoleDTO.Role);
        return Ok(new ApiSuccessResponse<IdentityResult>(data));
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(ApiSuccessResponse<List<UserInfoDTO>>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> GetUsers()
    {
        var data = await _userService.GetUsersAsync();

        return Ok(new ApiSuccessResponse<List<UserInfoDTO>>(data));
    }
}