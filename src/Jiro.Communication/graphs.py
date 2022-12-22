import plotext as plt
import json
import lib
from datetime import datetime

dateFormat = 'd/m/Y H:M:S'


def display_weather(jsonInput: str):
    weather = json.loads(jsonInput)
    dates: list[datetime] = [datetime.strptime(
        x, '%Y-%m-%dT%H:%M') for x in weather['hourly']['time']]

    datesFinal = plt.datetimes_to_string(dates, output_form=dateFormat)
    temperatures = weather['hourly']['temperature_2m']
    rain = weather['hourly']['rain']
    windspeed = weather['hourly']['windspeed_10m']
    # pressure = weather['hourly']['surface_pressure']

    plt.date_form(dateFormat)
    plt.plot(datesFinal, temperatures, marker='x',
             color='red', label='Temperature C°')
    plt.plot(datesFinal, rain, color='blue', label='Rain mm')
    plt.plot(datesFinal, windspeed, marker="~",
             color='magenta', label='Windspeed km/h')
    # plt.plot(datesFinal, pressure, color='green', label='Pressure')

    plt.theme('pro')
    plt.show()

    currentWeather = weather['current_weather']
    print(lib.colors.ENDC)

    print("Current weather:")
    print(f'Temperature: {currentWeather["temperature"]} C°')
    print(f'Wind speed: {currentWeather["windspeed"]} km/h')
