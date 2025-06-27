using System.Collections.Concurrent;

using Jiro.Core.Services.Semaphore;

using Microsoft.Extensions.Logging;

using Moq;

namespace Jiro.Tests.ServiceTests;

public class SemaphoreManagerTests
{
	private readonly Mock<ILogger<SemaphoreManager>> _loggerMock;
	private readonly ISemaphoreManager _semaphoreManager;

	public SemaphoreManagerTests ()
	{
		_loggerMock = new Mock<ILogger<SemaphoreManager>>();
		_semaphoreManager = new SemaphoreManager(_loggerMock.Object);
	}

	[Fact]
	public void GetOrCreateInstanceSemaphore_WithNewInstanceId_ShouldCreateNewSemaphore ()
	{
		// Arrange
		const string instanceId = "test-instance-1";

		// Act
		var semaphore = _semaphoreManager.GetOrCreateInstanceSemaphore(instanceId);

		// Assert
		Assert.NotNull(semaphore);
		Assert.Equal(1, semaphore.CurrentCount);
	}

	[Fact]
	public void GetOrCreateInstanceSemaphore_WithSameInstanceId_ShouldReturnSameSemaphore ()
	{
		// Arrange
		const string instanceId = "test-instance-2";

		// Act
		var semaphore1 = _semaphoreManager.GetOrCreateInstanceSemaphore(instanceId);
		var semaphore2 = _semaphoreManager.GetOrCreateInstanceSemaphore(instanceId);

		// Assert
		Assert.NotNull(semaphore1);
		Assert.NotNull(semaphore2);
		Assert.Same(semaphore1, semaphore2);
	}

	[Fact]
	public void GetOrCreateInstanceSemaphore_WithDifferentInstanceIds_ShouldReturnDifferentSemaphores ()
	{
		// Arrange
		const string instanceId1 = "test-instance-3";
		const string instanceId2 = "test-instance-4";

		// Act
		var semaphore1 = _semaphoreManager.GetOrCreateInstanceSemaphore(instanceId1);
		var semaphore2 = _semaphoreManager.GetOrCreateInstanceSemaphore(instanceId2);

		// Assert
		Assert.NotNull(semaphore1);
		Assert.NotNull(semaphore2);
		Assert.NotSame(semaphore1, semaphore2);
	}

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("test-instance")]
	[InlineData("complex-instance-id-with-dashes-123")]
	public void GetOrCreateInstanceSemaphore_WithVariousInstanceIds_ShouldCreateValidSemaphores (string instanceId)
	{
		// Act
		var semaphore = _semaphoreManager.GetOrCreateInstanceSemaphore(instanceId);

		// Assert
		Assert.NotNull(semaphore);
		Assert.Equal(1, semaphore.CurrentCount);
	}

	[Fact]
	public async Task GetOrCreateInstanceSemaphore_ConcurrentAccess_ShouldHandleProperly ()
	{
		// Arrange
		const string instanceId = "concurrent-test";
		const int taskCount = 10;
		var tasks = new List<Task<SemaphoreSlim>>();

		// Act
		for (int i = 0; i < taskCount; i++)
		{
			tasks.Add(Task.Run(() => _semaphoreManager.GetOrCreateInstanceSemaphore(instanceId)));
		}

		var results = await Task.WhenAll(tasks);

		// Assert
		Assert.All(results, semaphore => Assert.NotNull(semaphore));
		Assert.All(results, semaphore => Assert.Same(results[0], semaphore));
	}

	[Fact]
	public async Task GetOrCreateInstanceSemaphore_SemaphoreUsage_ShouldWorkCorrectly ()
	{
		// Arrange
		const string instanceId = "usage-test";
		var semaphore = _semaphoreManager.GetOrCreateInstanceSemaphore(instanceId);

		// Act & Assert
		Assert.Equal(1, semaphore.CurrentCount);

		await semaphore.WaitAsync();
		Assert.Equal(0, semaphore.CurrentCount);

		semaphore.Release();
		Assert.Equal(1, semaphore.CurrentCount);
	}
}
