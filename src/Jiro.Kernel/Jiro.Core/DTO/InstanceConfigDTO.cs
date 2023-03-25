using System.Text.Json.Serialization;

namespace Jiro.Core.DTO
{
    public class InstanceConfigDTO
    {
        [JsonPropertyName("urls")]
        public string urls { get; set; }

        [JsonPropertyName("TokenizerUrl")]
        public string TokenizerUrl { get; set; }

        [JsonPropertyName("Logging")]
        public Logging Logging { get; set; }

        [JsonPropertyName("ConnectionStrings")]
        public ConnectionStrings ConnectionStrings { get; set; }

        [JsonPropertyName("Log")]
        public Log Log { get; set; }

        [JsonPropertyName("Whitelist")]
        public bool? Whitelist { get; set; }

        [JsonPropertyName("JWT")]
        public JWT JWT { get; set; }

        [JsonPropertyName("Gpt")]
        public Gpt Gpt { get; set; }

        [JsonPropertyName("AllowedHosts")]
        public string AllowedHosts { get; set; }
    }

    public class ChatGpt
    {
        [JsonPropertyName("SystemMessage")]
        public string SystemMessage { get; set; }
    }

    public class ConnectionStrings
    {
        [JsonPropertyName("JiroContext")]
        public string JiroContext { get; set; }
    }

    public class Gpt
    {
        [JsonPropertyName("Enable")]
        public bool? Enable { get; set; }

        [JsonPropertyName("BaseUrl")]
        public string BaseUrl { get; set; }

        [JsonPropertyName("AuthToken")]
        public string AuthToken { get; set; }

        [JsonPropertyName("Organization")]
        public string Organization { get; set; }

        [JsonPropertyName("FineTune")]
        public bool? FineTune { get; set; }

        [JsonPropertyName("UseChatGpt")]
        public bool? UseChatGpt { get; set; }

        [JsonPropertyName("ChatGpt")]
        public ChatGpt ChatGpt { get; set; }

        [JsonPropertyName("SingleGpt")]
        public SingleGpt SingleGpt { get; set; }
    }

    public class JWT
    {
        [JsonPropertyName("Issuer")]
        public string Issuer { get; set; }

        [JsonPropertyName("Audience")]
        public string Audience { get; set; }

        [JsonPropertyName("Secret")]
        public string Secret { get; set; }

        [JsonPropertyName("AccessTokenExpireTime")]
        public int? AccessTokenExpireTime { get; set; }

        [JsonPropertyName("RefreshTokenExpireTime")]
        public int? RefreshTokenExpireTime { get; set; }
    }

    public class Log
    {
        [JsonPropertyName("TimeInterval")]
        public string TimeInterval { get; set; }

        [JsonPropertyName("LogLevel")]
        public string LogLevel { get; set; }

        [JsonPropertyName("AspNetCoreLevel")]
        public string AspNetCoreLevel { get; set; }

        [JsonPropertyName("DatabaseLevel")]
        public string DatabaseLevel { get; set; }

        [JsonPropertyName("SystemLevel")]
        public string SystemLevel { get; set; }
    }

    public class Logging
    {
        [JsonPropertyName("LogLevel")]
        public LogLevel LogLevel { get; set; }
    }

    public class LogLevel
    {
        [JsonPropertyName("Default")]
        public string Default { get; set; }

        [JsonPropertyName("Microsoft.AspNetCore")]
        public string MicrosoftAspNetCore { get; set; }

        [JsonPropertyName("System")]
        public string System { get; set; }

        [JsonPropertyName("Microsoft.EntityFrameworkCore.Database.Command")]
        public string MicrosoftEntityFrameworkCoreDatabaseCommand { get; set; }
    }

    public class SingleGpt
    {
        [JsonPropertyName("TokenLimit")]
        public int? TokenLimit { get; set; }

        [JsonPropertyName("ContextMessage")]
        public string ContextMessage { get; set; }

        [JsonPropertyName("Stop")]
        public string Stop { get; set; }

        [JsonPropertyName("Model")]
        public string Model { get; set; }
    }
}