using System.Net;
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
                    HandledException => (int)HttpStatusCode.BadRequest,
                    HandledExceptionList => (int)HttpStatusCode.BadRequest,
                    TokenException => (int)HttpStatusCode.Unauthorized,
                    _ => (int)HttpStatusCode.BadRequest
                };

                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(result);
            }
        }

        public ApiResponse<object> GetExceptionResponse(Exception exception)
        {
            ApiResponse<object> errorResult = exception switch
            {
                HandledException => new()
                {
                    Data = default,
                    Errors = new string[] { exception.Message },
                    IsSuccess = false
                },
                HandledExceptionList list => new()
                {
                    Data = default,
                    Errors = list!.ExceptionMessages!,
                    IsSuccess = false
                },
                TokenException => new()
                {
                    Data = default,
                    Errors = new string[] { exception.Message },
                    IsSuccess = false
                },
                _ => new()
                {
                    Data = default,
                    Errors = new string[] { exception.Message },
                    IsSuccess = false
                },
            };

            return errorResult;
        }
    }
}