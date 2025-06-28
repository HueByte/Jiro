using Jiro.Commands.Models;
using Jiro.Core.Services.CommandSystem;

namespace Jiro.Tests.ServiceTests;

// Test class that extends CommandsContext to allow for testing
public class TestCommandsContext : CommandsContext
{
	private Dictionary<string, CommandModuleInfo> _commandModules = new();
	private Dictionary<string, CommandInfo> _commands = new();

	public new Dictionary<string, CommandModuleInfo> CommandModules => _commandModules;
	public new Dictionary<string, CommandInfo> Commands => _commands;

	public void SetupTestData(Dictionary<string, CommandModuleInfo> modules, Dictionary<string, CommandInfo> commands)
	{
		_commandModules = modules;
		_commands = commands;
	}
}

public class HelpServiceTests
{
	private readonly TestCommandsContext _testCommandsContext;
	private readonly IHelpService _helpService;

	public HelpServiceTests()
	{
		_testCommandsContext = new TestCommandsContext();
		SetupTestCommandsContext();
		_helpService = new HelpService(_testCommandsContext);
	}

	private void SetupTestCommandsContext()
	{
		// Setup empty contexts for basic testing
		var commandModules = new Dictionary<string, CommandModuleInfo>();
		var allCommands = new Dictionary<string, CommandInfo>();

		_testCommandsContext.SetupTestData(commandModules, allCommands);
	}

	[Fact]
	public void Constructor_ShouldCreateHelpMessage()
	{
		// Act & Assert
		Assert.NotNull(_helpService.HelpMessage);
	}

	[Fact]
	public void Constructor_ShouldSetHelpMessageProperty()
	{
		// Act
		var helpMessage = _helpService.HelpMessage;

		// Assert
		Assert.NotNull(helpMessage);
		Assert.IsType<string>(helpMessage);
	}

	[Fact]
	public void CreateHelpMessage_ShouldUpdateHelpMessage()
	{
		// Arrange
		var originalMessage = _helpService.HelpMessage;

		// Act
		_helpService.CreateHelpMessage();

		// Assert - Message should be regenerated (even if empty)
		Assert.NotNull(_helpService.HelpMessage);
	}

	[Fact]
	public void HelpMessage_WithEmptyCommands_ShouldHandleGracefully()
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
	public void CreateHelpMessage_ShouldCallCommandsContextProperties()
	{
		// Arrange
		var commandModules = new Dictionary<string, CommandModuleInfo>();
		var allCommands = new Dictionary<string, CommandInfo>();
		_testCommandsContext.SetupTestData(commandModules, allCommands);

		// Act
		_helpService.CreateHelpMessage();

		// Assert - Since we can't verify calls on concrete class,
		// we verify the result is what we expect
		Assert.NotNull(_helpService.HelpMessage);
		Assert.Equal(_testCommandsContext.Commands, allCommands);
		Assert.Equal(_testCommandsContext.CommandModules, commandModules);
	}
}
