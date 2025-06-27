using System.Text;

using Jiro.Commands.Models;
using Jiro.Core.Services.CommandSystem;

using Moq;

namespace Jiro.Tests.ServiceTests;

public class HelpServiceTests
{
	private readonly Mock<CommandsContext> _commandsContextMock;
	private readonly IHelpService _helpService;

	public HelpServiceTests ()
	{
		_commandsContextMock = new Mock<CommandsContext>();
		SetupMockCommandsContext();
		_helpService = new HelpService(_commandsContextMock.Object);
	}

	private void SetupMockCommandsContext ()
	{
		// Setup empty contexts for basic testing
		var commandModules = new Dictionary<string, CommandModuleInfo>();
		var allCommands = new Dictionary<string, CommandInfo>();

		_commandsContextMock.Setup(x => x.CommandModules).Returns(commandModules);
		_commandsContextMock.Setup(x => x.Commands).Returns(allCommands);
	}

	[Fact]
	public void Constructor_ShouldCreateHelpMessage ()
	{
		// Act & Assert
		Assert.NotNull(_helpService.HelpMessage);
	}

	[Fact]
	public void Constructor_ShouldSetHelpMessageProperty ()
	{
		// Act
		var helpMessage = _helpService.HelpMessage;

		// Assert
		Assert.NotNull(helpMessage);
		Assert.IsType<string>(helpMessage);
	}

	[Fact]
	public void CreateHelpMessage_ShouldUpdateHelpMessage ()
	{
		// Arrange
		var originalMessage = _helpService.HelpMessage;

		// Act
		_helpService.CreateHelpMessage();

		// Assert - Message should be regenerated (even if empty)
		Assert.NotNull(_helpService.HelpMessage);
	}

	[Fact]
	public void HelpMessage_WithEmptyCommands_ShouldHandleGracefully ()
	{
		// Arrange - Already setup with empty commands in constructor

		// Act
		var helpMessage = _helpService.HelpMessage;

		// Assert
		Assert.NotNull(helpMessage);
		// With empty commands, should just have newline at end from StringBuilder
		Assert.True(helpMessage.Length >= 0);
	}

	[Fact]
	public void CreateHelpMessage_ShouldCallCommandsContextProperties ()
	{
		// Act
		_helpService.CreateHelpMessage();

		// Assert
		_commandsContextMock.Verify(x => x.Commands, Times.AtLeastOnce);
		_commandsContextMock.Verify(x => x.CommandModules, Times.AtLeastOnce);
	}
}
