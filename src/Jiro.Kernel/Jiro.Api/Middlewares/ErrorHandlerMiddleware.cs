using System.Net;
using Jiro.Commands.Exceptions;
using Jiro.Core;

namespace Jiro.Api.Middlewares;

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
            var result = GetExceptionResponse(ex, context.Response);

            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(result);
        }
    }

    public static ApiErrorResponse GetExceptionResponse(Exception exception, HttpResponse response)
    {
        ApiErrorResponse errorResult = exception switch
        {
            JiroException ex => new()
            {
                Code = (int)HttpStatusCode.BadRequest,
                Message = ex.UserMessage,
                Details = ex.Details
            },
            TokenException ex => new()
            {
                Code = 498,
                Message = ex.Message,
                Details = null
            },
            _ => new()
            {
                Code = (int)HttpStatusCode.BadRequest,
                Message = "Something went wrong",
                Details = null
            }
        };

        response.StatusCode = errorResult.Code;

        return errorResult;
    }
}