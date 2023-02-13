using System.Resources;
using Jiro.Core.Base;
using Jiro.Core.Base.Attributes;
using Jiro.Core.Interfaces.IServices;
using Microsoft.Extensions.DependencyInjection;

namespace Jiro.Core.Commands.Weather
{
    [CommandModule("Weather")]
    public class WeatherCommand : ICommandBase
    {
        private readonly IWeatherService _weatherService;
        public WeatherCommand(IWeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        [Command("weather")]
        public async Task<ICommandResult> Weather(string location)
        {
            var result = await _weatherService.GetWeatherAsync(location);

            return CommandResult.Create(result);
        }
    }
}