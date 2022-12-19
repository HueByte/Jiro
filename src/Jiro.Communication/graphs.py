import plotext as plt
import json
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
    plt.plot(datesFinal, temperatures, color='red', label='Temperature')
    plt.plot(datesFinal, rain, color='blue', label='Rain')
    plt.plot(datesFinal, windspeed, color='magenta', label='Windspeed')
    # plt.plot(datesFinal, pressure, color='green', label='Pressure')
    plt.show()
