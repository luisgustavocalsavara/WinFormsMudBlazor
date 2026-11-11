using WinFormsMudBlazor.Models.Entities;
using WinFormsMudBlazor.Models.Interfaces;

namespace WinFormsMudBlazor.Services;

public class WeatherService : IWeatherService
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild",
        "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    public async Task<IReadOnlyList<WeatherForecast>> GetForecastsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(500, cancellationToken);

        var startDate = DateOnly.FromDateTime(DateTime.Now);
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = startDate.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        }).ToArray();
    }
}
