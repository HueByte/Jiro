using System.Net;
using Jiro.Commands.Exceptions;
using Jiro.Core;

namespace Jiro.Api.Middlewares
{
    public static class ErrorHandlerExtensions
    {
        public static IApplicationBuilder UseErrorHandler(this IApplicationBuilder app)
            => app.UseMiddleware<ErrorHandlerMiddleware>();
    }

    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        public ErrorHandlerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var result = GetExceptionResponse(ex);

                context.Response.StatusCode = ex switch
                {
                    CommandException => (int)HttpStatusCode.BadRequest,
                    HandledException => (int)HttpStatusCode.BadRequest,
                    HandledExceptionList => (int)HttpStatusCode.BadRequest,
                    TokenException => 498,
                    _ => (int)HttpStatusCode.BadRequest
                };

                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(result);
            }
        }

        public static ApiResponse<object> GetExceptionResponse(Exception exception)
        {
            ApiResponse<object> errorResult = exception switch
            {
                CommandException ex => new()
                {
                    Data = ex.CommandName,
                    Errors = new string[] { exception.Message },
                    IsSuccess = false
                },
                HandledException or
                HandledExceptionList or
                TokenException or
                _ => new()
                {
                    Data = default,
                    Errors = new string[] { exception.Message },
                    IsSuccess = false
                }
            };

            return errorResult;
        }
    }
}