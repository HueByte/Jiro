using System.Text.Json.Serialization;

namespace Jiro.Api.Base
{
    public class ApiResponse<T>
    {
        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("errors")]
        public ICollection<string?>? Errors { get; set; }

        [JsonPropertyName("isSuccess")]
        public bool IsSuccess { get; set; }

        public ApiResponse() { }
        public ApiResponse(T? data) : this(data, null!, true) { }
        public ApiResponse(T? data, ICollection<string>? errors, bool isSuccess)
        {
            Data = data;
            Errors = errors!;
            IsSuccess = isSuccess;
        }
    }
}