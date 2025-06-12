namespace Jiro.Core.Interfaces.IServices;

public interface ICommandContext
{
    string? UserId
    {
        get;
    }
    Dictionary<string, object> Data
    {
        get;
    }
    void SetData(IEnumerable<KeyValuePair<string, object>> data);
    void SetCurrentUser(string? userId);
}
