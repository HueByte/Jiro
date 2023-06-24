using Jiro.Core;
using Jiro.Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Jiro.Api.Controllers
{
    public class AuthController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ICurrentUserService _currentUserService;
        public AuthController(IUserService userService, IRefreshTokenService refreshTokenService, ICurrentUserService currentUserService)
        {
            _userService = userService;
            _refreshTokenService = refreshTokenService;
            _currentUserService = currentUserService;
        }

        [HttpPost("create")]
        [ProducesResponseType(typeof(ApiResponse<IdentityResult>), 200)]
        public async Task<IActionResult> CreateAccount([FromBody] RegisterDTO registerDTO)
        {
            var data = await _userService.CreateUserAsync(registerDTO);

            return ApiResponseCreator.Data(data);
        }

        [HttpDelete("deleteUser")]
        [Authorize(Roles = Roles.ADMIN)]
        [ProducesResponseType(typeof(ApiResponse<IdentityResult>), 200)]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var data = await _userService.DeleteUserAsync(userId);

            return ApiResponseCreator.Data(data);
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<VerifiedUser>), 200)]
        public async Task<IActionResult> Login([FromBody] LoginUsernameDTO userDTO)
        {
            var data = await _userService.LoginUserAsync(userDTO, GetIpAddress()!);
            var result = new ApiResponse<VerifiedUser>(data);

            if (result.IsSuccess)
            {
                AttachAuthCookies(result.Data!);
            }

            return ApiResponseCreator.Create(result);
        }

        [HttpPost("refreshToken")]
        [ProducesResponseType(typeof(ApiResponse<VerifiedUser>), 200)]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = Request.Cookies[CookieNames.REFRESH_TOKEN];
            var data = await _refreshTokenService.RefreshToken(refreshToken!, GetIpAddress()!);

            var result = new ApiResponse<VerifiedUser>(data);

            if (result.IsSuccess && !string.IsNullOrEmpty(result.Data?.RefreshToken))
            {
                AttachAuthCookies(result.Data!);
            }

            return ApiResponseCreator.Create(result);
        }

        [HttpPost("revokeToken")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> RevokeToken([FromBody] string bodyToken)
        {
            var token = bodyToken ?? Request.Cookies[CookieNames.REFRESH_TOKEN];

            await _refreshTokenService.RevokeToken(token!, GetIpAddress()!);

            return ApiResponseCreator.Empty();
        }

        [HttpPost("logout")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies[CookieNames.REFRESH_TOKEN];

            if (refreshToken is not null)
                await _refreshTokenService.RevokeToken(refreshToken!, GetIpAddress()!);

            Response.Cookies.Delete(CookieNames.REFRESH_TOKEN);
            Response.Cookies.Delete(CookieNames.ACCESS_TOKEN);

            return ApiResponseCreator.Empty();
        }

        [HttpPost("changePassword")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO changePasswordDTO)
        {
            var data = await _userService.ChangePasswordAsync(_currentUserService.UserId, changePasswordDTO.CurrentPassword, changePasswordDTO.NewPassword);

            return ApiResponseCreator.ValueType(data);
        }

        [HttpPost("changeEmail")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailDTO changeEmailDTO)
        {
            var data = await _userService.ChangeEmailAsync(_currentUserService.UserId, changeEmailDTO.Password, changeEmailDTO.NewEmail);

            return ApiResponseCreator.ValueType(data);
        }

        private string? GetIpAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request?.Headers["X-Forwarded-For"] ?? "localhost";
            else
                return HttpContext.Connection?.RemoteIpAddress?.MapToIPv4().ToString() ?? "localhost";
        }

        private void AttachAuthCookies(VerifiedUser user)
        {
            if (user is null) return;

            var refreshTokenOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = user.RefreshTokenExpiration
            };

            var jwtTokenOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = user.AccessTokenExpiration,
            };

            Response.Cookies.Append(CookieNames.REFRESH_TOKEN, user.RefreshToken!, refreshTokenOptions);
            Response.Cookies.Append(CookieNames.ACCESS_TOKEN, user.Token!, jwtTokenOptions);
        }
    }
}