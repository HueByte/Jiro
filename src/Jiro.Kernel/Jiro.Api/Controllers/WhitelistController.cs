using Jiro.Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jiro.Api.Controllers
{
    [Authorize(Roles = Roles.ADMIN, Policy = Jiro.Core.Constants.Policies.WHITE_LIST)]
    public class WhitelistController : BaseController
    {
        private readonly IWhitelistService _whitelistService;
        public WhitelistController(IWhitelistService whitelistService)
        {
            _whitelistService = whitelistService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> AddUserToWhitelist([FromBody] string userId)
        {
            var result = await _whitelistService.AddUserToWhitelist(userId);

            return Ok(result);
        }

        [HttpDelete]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> RemoveUserToWhitelist([FromBody] string userId)
        {
            var result = await _whitelistService.RemoveUserToWhitelist(userId);

            return Ok(result);
        }
    }
}