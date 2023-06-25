using System.Security.Claims;

namespace Jiro.Api.Middlewares
{
    public static class CurrentUserExtensions
    {
        public static IApplicationBuilder UseCurrentUser(this IApplicationBuilder app)
            => app.UseMiddleware<CurrentUserMiddleware>();
    }

    public class CurrentUserMiddleware
    {
        private readonly RequestDelegate _next;
        public CurrentUserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUserService)
        {
            if (context.User.Identity?.IsAuthenticated == true)
                currentUserService.SetCurrentUser(context.User.FindFirstValue(ClaimTypes.NameIdentifier));

            await _next(context);
        }
    }
}