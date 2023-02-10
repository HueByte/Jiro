using Jiro.Core;
using Microsoft.AspNetCore.Mvc;

namespace Jiro.Api
{
    public static class ApiResponseCreator
    {
        public static IActionResult Error(ICollection<string> errors)
        {
            ApiResponse<object> result = new(default, errors, false);

            return new OkObjectResult(result);
        }

        public static IActionResult Empty()
        {
            ApiResponse<object> result = new(default, null!, true);
            return new OkObjectResult(result);
        }

        public static IActionResult Data<T>(T? data) where T : class
        {
            ApiResponse<T> result = new(data);
            return Create(result);
        }

        public static IActionResult ValueType<T>(T data) where T : struct
        {
            if (!typeof(T).IsPrimitive) throw new HandledException($"{data.GetType()} is not primitive data type");

            ApiResponse<T> result = new(data);
            return Create(result);
        }

        public static IActionResult Property<T>(T data)
        {
            ApiResponse<T> result = new(data);
            return Create(result);
        }

        public static IActionResult Create<T>(ApiResponse<T> result) => new OkObjectResult(result);
    }
}