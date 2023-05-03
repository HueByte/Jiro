namespace Jiro.Core.DTO
{
    public class UserInfoDTO
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string[]? Roles { get; set; }
        public DateTime AccountCreatedDate { get; set; }
        public bool IsWhitelisted { get; set; }
    }
}