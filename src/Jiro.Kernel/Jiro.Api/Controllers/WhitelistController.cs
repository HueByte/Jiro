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
        [ProducesResponseType(typeof(ApiSuccessResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        public async Task<IActionResult> AddUserToWhitelist([FromBody] UserIdDTO user)
        {
            var result = await _whitelistService.AddUserToWhitelistAsync(user.UserId);

            return Ok(new ApiSuccessResponse<object>(result));
        }

        [HttpDelete]
        [ProducesResponseType(typeof(ApiSuccessResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        public async Task<IActionResult> RemoveUserToWhitelist([FromBody] UserIdDTO user)
        {
            var result = await _whitelistService.RemoveUserFromWhitelistAsync(user.UserId);

            return Ok(new ApiSuccessResponse<object>(result));
        }

        [HttpGet("whitelistedUsers")]
        [ProducesResponseType(typeof(ApiSuccessResponse<List<WhitelistedUserDTO>>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        public async Task<IActionResult> GetWhitelistedUsers()
        {
            var result = await _whitelistService.GetWhiteListUsersAsync();

            return Ok(new ApiSuccessResponse<List<WhitelistedUserDTO>>(result));
        }

        [HttpPut("whitelistedUsers")]
        [ProducesResponseType(typeof(ApiSuccessResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        public async Task<IActionResult> UpdateWhitelistedUsers([FromBody] IEnumerable<WhitelistedUserDTO> users)
        {
            var result = await _whitelistService.UpdateWhitelistRangeAsync(users);

            return Ok(new ApiSuccessResponse<bool>(result));
        }

        [HttpGet("users")]
        [ProducesResponseType(typeof(ApiSuccessResponse<List<WhitelistedUserDTO>>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        public async Task<IActionResult> GetUsersWithWhitelistFlag()
        {
            var result = await _whitelistService.GetUsersWithWhitelistFlagAsync();

            return Ok(new ApiSuccessResponse<List<WhitelistedUserDTO>>(result));
        }
    }
}