using System.Text.Json.Serialization;

namespace Jiro.Api.Base;

public class ApiSuccessResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    public ApiSuccessResponse() { }
    public ApiSuccessResponse(T? data) => Data = data;
}

public class ApiErrorResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("details")]
    public string[]? Details { get; set; }
}