using Jiro.Core.Constants;
using Jiro.Core.Services.Auth.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Jiro.Api.Controllers;

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
    [ProducesResponseType(typeof(ApiSuccessResponse<IdentityResult>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> CreateAccount([FromBody] RegisterDTO registerDTO)
    {
        var data = await _userService.CreateUserAsync(registerDTO);

        return Ok(new ApiSuccessResponse<IdentityResult>(data));
    }

    [HttpDelete("deleteUser")]
    [Authorize(Roles = Roles.ADMIN)]
    [ProducesResponseType(typeof(ApiSuccessResponse<IdentityResult>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var data = await _userService.DeleteUserAsync(userId);

        return Ok(new ApiSuccessResponse<IdentityResult>(data));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiSuccessResponse<VerifiedUser>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> Login([FromBody] LoginUsernameDTO userDTO)
    {
        var data = await _userService.LoginUserAsync(userDTO, GetIpAddress()!);
        AttachAuthCookies(data);

        return Ok(new ApiSuccessResponse<VerifiedUser>(data));
    }

    [HttpPost("refreshToken")]
    [ProducesResponseType(typeof(ApiSuccessResponse<VerifiedUser>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> RefreshToken()
    {
        var refreshToken = Request.Cookies[CookieNames.REFRESH_TOKEN];
        var data = await _refreshTokenService.RefreshToken(refreshToken!, GetIpAddress()!);

        AttachAuthCookies(data);

        return Ok(new ApiSuccessResponse<VerifiedUser>(data));
    }

    [HttpPost("revokeToken")]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> RevokeToken([FromBody] string bodyToken)
    {
        var token = bodyToken ?? Request.Cookies[CookieNames.REFRESH_TOKEN];

        await _refreshTokenService.RevokeToken(token!, GetIpAddress()!);

        return Ok(new ApiSuccessResponse<object>(null));
    }

    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies[CookieNames.REFRESH_TOKEN];

        if (refreshToken is not null)
            await _refreshTokenService.RevokeToken(refreshToken!, GetIpAddress()!);

        Response.Cookies.Delete(CookieNames.REFRESH_TOKEN);
        Response.Cookies.Delete(CookieNames.ACCESS_TOKEN);

        return Ok(new ApiSuccessResponse<object>(null));
    }

    [HttpPost("changePassword")]
    [Authorize]
    [ProducesResponseType(typeof(ApiSuccessResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO changePasswordDTO)
    {
        var data = await _userService.ChangePasswordAsync(_currentUserService.UserId, changePasswordDTO.CurrentPassword, changePasswordDTO.NewPassword);

        return Ok(new ApiSuccessResponse<bool>(data));
    }

    [HttpPost("changeEmail")]
    [Authorize]
    [ProducesResponseType(typeof(ApiSuccessResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailDTO changeEmailDTO)
    {
        var data = await _userService.ChangeEmailAsync(_currentUserService.UserId, changeEmailDTO.Password, changeEmailDTO.NewEmail);

        return Ok(new ApiSuccessResponse<bool>(data));
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