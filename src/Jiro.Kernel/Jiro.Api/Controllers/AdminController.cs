using Jiro.Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Jiro.Api.Controllers
{
    [Authorize(Roles = Roles.ADMIN)]
    public class AdminController : BaseController
    {
        private readonly IUserService _userService;
        public AdminController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("assingRole")]
        [ProducesResponseType(typeof(ApiResponse<IdentityResult>), 200)]
        public async Task<IActionResult> AssingRole([FromBody] AssignRoleDTO assignRoleDTO)
        {
            var data = await _userService.AssignRoleAsync(assignRoleDTO.UserId, assignRoleDTO.Role);
            return ApiResponseCreator.Data(data);
        }
    }
}