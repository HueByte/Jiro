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
        public AuthController(IUserService userService, IRefreshTokenService refreshTokenService)
        {
            _userService = userService;
            _refreshTokenService = refreshTokenService;
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
        [ProducesResponseType(typeof(ApiResponse<VerifiedUserDTO>), 200)]
        public async Task<IActionResult> Login([FromBody] LoginUsernameDTO userDTO)
        {
            var data = await _userService.LoginUserAsync(userDTO, GetIpAddress());
            var result = new ApiResponse<VerifiedUserDTO>(data);

            if (result.IsSuccess)
            {
                AttachAuthCookies(result.Data!);
            }

            return ApiResponseCreator.Create(result);
        }

        [HttpPost("refreshToken")]
        [ProducesResponseType(typeof(ApiResponse<VerifiedUserDTO>), 200)]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = Request.Cookies[CookieNames.REFRESH_TOKEN];
            var data = await _refreshTokenService.RefreshToken(refreshToken!, GetIpAddress());

            var result = new ApiResponse<VerifiedUserDTO>(data);

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

            await _refreshTokenService.RevokeToken(token!, GetIpAddress());

            return ApiResponseCreator.Empty();
        }

        [HttpPost("logout")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies[CookieNames.REFRESH_TOKEN];

            if (refreshToken is not null)
                await _refreshTokenService.RevokeToken(refreshToken!, GetIpAddress());

            Response.Cookies.Delete(CookieNames.REFRESH_TOKEN);
            Response.Cookies.Delete(CookieNames.ACCESS_TOKEN);

            return ApiResponseCreator.Empty();
        }

        private string GetIpAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request?.Headers["X-Forwarded-For"] ?? "localhost";
            else
                return HttpContext.Connection?.RemoteIpAddress?.MapToIPv4().ToString() ?? "localhost";
        }

        private void AttachAuthCookies(VerifiedUserDTO user)
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