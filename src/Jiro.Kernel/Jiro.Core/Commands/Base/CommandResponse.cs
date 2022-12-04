namespace Jiro.Core.Commands.Base
{
    public class CommandResponse
    {
        public object? Data { get; set; }
        public bool IsSuccess { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}