using System.ComponentModel;
using Jiro.Core.Attributes;
using Jiro.Core.Commands.Base;
using Microsoft.Extensions.Logging;

namespace Jiro.Core.Commands.Test.GPTCommand
{
    public record TestResult(string? message);

    [CommandContainer("Test")]
    public class TestCommand : CommandBase
    {
        private readonly ILogger _logger;
        public TestCommand(ILogger<TestCommand> logger)
        {
            _logger = logger;
        }

        [Command("TestingCommand")]
        public async Task<TestResult> TestingComamand()
        {
            await Task.Delay(1000);
            _logger.LogInformation("Test command run");
            return new("Success");
        }
    }
}