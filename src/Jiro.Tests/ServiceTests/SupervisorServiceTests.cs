using Jiro.Core.Services.Supervisor;

namespace Jiro.Tests.ServiceTests;

public class SupervisorServiceTests
{
	private readonly SupervisorService _supervisorService;

	public SupervisorServiceTests()
	{
		_supervisorService = new SupervisorService();
	}

	[Fact]
	public void Constructor_ShouldCreateInstance()
	{
		// Arrange & Act
		var service = new SupervisorService();

		// Assert
		Assert.NotNull(service);
	}

	[Fact]
	public void OnConnected_ShouldAllowEventSubscription()
	{
		// Arrange
		Func<Task> handler = static () => Task.CompletedTask;

		// Act - Events should allow subscription without throwing
		_supervisorService.OnConnected += handler;

		// Assert - If we get here without exception, the event subscription worked
		Assert.True(true);

		// Cleanup
		_supervisorService.OnConnected -= handler;
	}

	[Fact]
	public void OnDisconnected_ShouldAllowEventSubscription()
	{
		// Arrange
		Func<Task> handler = static () => Task.CompletedTask;

		// Act - Events should allow subscription without throwing
		_supervisorService.OnDisconnected += handler;

		// Assert - If we get here without exception, the event subscription worked
		Assert.True(true);

		// Cleanup
		_supervisorService.OnDisconnected -= handler;
	}

	[Fact]
	public void OnMessageReceived_ShouldAllowEventSubscription()
	{
		// Arrange
		Func<string, Task> handler = static (message) => Task.CompletedTask;

		// Act - Events should allow subscription without throwing
		_supervisorService.OnMessageReceived += handler;

		// Assert - If we get here without exception, the event subscription worked
		Assert.True(true);

		// Cleanup
		_supervisorService.OnMessageReceived -= handler;
	}

	[Fact]
	public void OnMessageSent_ShouldAllowEventSubscription()
	{
		// Arrange
		Func<string, Task> handler = static (message) => Task.CompletedTask;

		// Act - Events should allow subscription without throwing
		_supervisorService.OnMessageSent += handler;

		// Assert - If we get here without exception, the event subscription worked
		Assert.True(true);

		// Cleanup
		_supervisorService.OnMessageSent -= handler;
	}
}
