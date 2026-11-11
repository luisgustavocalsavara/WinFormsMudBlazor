using WinFormsMudBlazor.Models.Entities;

namespace WinFormsMudBlazor.Models.Interfaces;

public interface IWeatherService
{
    Task<IReadOnlyList<WeatherForecast>> GetForecastsAsync(CancellationToken cancellationToken = default);
}
