using Jiro.Core.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Jiro.Api.Controllers
{
    public class AuthController : BaseController
    {
        public AuthController()
        {

        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateAccount()
        {
            return Ok();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login()
        {
            return Ok();
        }

        [HttpPost("refreshToken")]
        public async Task<IActionResult> RefreshToken()
        {
            return Ok();
        }

        [HttpPost("revokeToken")]
        public async Task<IActionResult> RevokeToken()
        {
            return Ok();
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            return Ok();
        }

        private void AttachAuthCookies()
        {

        }

        private string? GetIpAddress()
        {
            // get source ip address for the current request
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request?.Headers["X-Forwarded-For"]!;
            else
                return HttpContext.Connection?.RemoteIpAddress?.MapToIPv4().ToString();
        }
    }
}