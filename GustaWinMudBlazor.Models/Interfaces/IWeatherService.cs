using GustaMudBlazor.Models.Entities;

namespace GustaMudBlazor.Models.Interfaces;

public interface IWeatherService
{
    Task<IReadOnlyList<WeatherForecast>> GetForecastsAsync(CancellationToken cancellationToken = default);
}
