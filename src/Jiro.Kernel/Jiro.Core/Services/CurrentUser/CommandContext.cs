namespace Jiro.Core.Services.CurrentUser;

public class CommandContext : ICommandContext
{
    public string? UserId { get; private set; }
    public Dictionary<string, object> Data { get; } = new();

    public void SetData(IEnumerable<KeyValuePair<string, object>> data)
    {
        foreach (var (key, value) in data)
        {
            if (Data.ContainsKey(key))
                Data[key] = value;
            else
                Data.Add(key, value);
        }
    }

    public void SetCurrentUser(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
            throw new JiroException(new ArgumentException(null, nameof(userId)), "Something went wrong with parsing current user", "Try to relogin");

        UserId = userId;
    }
}
