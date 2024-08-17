namespace Jiro.Core.Services.Supervisor;

public class SupervisorService
{
    public event Func<Task>? OnConnected = null;
    public event Func<Task>? OnDisconnected = null;
    public event Func<string, Task>? OnMessageReceived = null;
    public event Func<string, Task>? OnMessageSent = null;


}