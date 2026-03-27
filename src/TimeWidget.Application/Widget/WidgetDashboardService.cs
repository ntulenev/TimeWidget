using Microsoft.Extensions.Options;

using TimeWidget.Application.Abstractions;
using TimeWidget.Application.Calendar;
using TimeWidget.Application.Clock;
using TimeWidget.Application.Weather;
using TimeWidget.Domain.Clock;
using TimeWidget.Domain.Configuration;
using TimeWidget.Domain.Location;
using TimeWidget.Domain.Widget;

namespace TimeWidget.Application.Widget;

public sealed class WidgetDashboardService
{
    private readonly IClockService _clockService;
    private readonly ICalendarService _calendarService;
    private readonly ILocationService _locationService;
    private readonly IWeatherService _weatherService;
    private readonly IWidgetPlacementStore _placementStore;
    private readonly ClockDisplayBuilder _clockDisplayBuilder;
    private readonly CalendarDisplayBuilder _calendarDisplayBuilder;
    private readonly WeatherDisplayBuilder _weatherDisplayBuilder;
    private readonly ClockCitiesSettings _clockCitiesSettings;
    private readonly GoogleCalendarSettings _googleCalendarSettings;

    private Coordinates? _weatherCoordinates;
    private bool _isRefreshingCalendar;
    private bool _isRefreshingWeather;
    private WeatherDisplayState _lastWeatherDisplay = new(string.Empty, string.Empty, "Locating...", false);
    private CalendarDisplayState _lastCalendarDisplay = new([], string.Empty, false, false, false, false, false);

    public WidgetDashboardService(
        IClockService clockService,
        ICalendarService calendarService,
        ILocationService locationService,
        IWeatherService weatherService,
        IWidgetPlacementStore placementStore,
        ClockDisplayBuilder clockDisplayBuilder,
        CalendarDisplayBuilder calendarDisplayBuilder,
        WeatherDisplayBuilder weatherDisplayBuilder,
        IOptions<ClockCitiesSettings> clockCitiesOptions,
        IOptions<GoogleCalendarSettings> googleCalendarOptions)
    {
        ArgumentNullException.ThrowIfNull(clockService);
        ArgumentNullException.ThrowIfNull(calendarService);
        ArgumentNullException.ThrowIfNull(locationService);
        ArgumentNullException.ThrowIfNull(weatherService);
        ArgumentNullException.ThrowIfNull(placementStore);
        ArgumentNullException.ThrowIfNull(clockDisplayBuilder);
        ArgumentNullException.ThrowIfNull(calendarDisplayBuilder);
        ArgumentNullException.ThrowIfNull(weatherDisplayBuilder);
        ArgumentNullException.ThrowIfNull(clockCitiesOptions);
        ArgumentNullException.ThrowIfNull(googleCalendarOptions);

        _clockService = clockService;
        _calendarService = calendarService;
        _locationService = locationService;
        _weatherService = weatherService;
        _placementStore = placementStore;
        _clockDisplayBuilder = clockDisplayBuilder;
        _calendarDisplayBuilder = calendarDisplayBuilder;
        _weatherDisplayBuilder = weatherDisplayBuilder;
        _clockCitiesSettings = clockCitiesOptions.Value;
        _googleCalendarSettings = googleCalendarOptions.Value;
    }

    public bool IsCalendarEnabled => _calendarService.IsEnabled;

    public TimeSpan CalendarRefreshInterval => TimeSpan.FromMinutes(_googleCalendarSettings.RefreshMinutes);

    public ClockDisplayState GetClockDisplayState() =>
        _clockDisplayBuilder.Build(_clockService.Now, _clockCitiesSettings);

    public async Task<WeatherRefreshResult> RefreshWeatherAsync(CancellationToken cancellationToken)
    {
        if (_isRefreshingWeather)
        {
            return new WeatherRefreshResult(_lastWeatherDisplay, false);
        }

        _isRefreshingWeather = true;

        try
        {
            var weatherResult = await LoadWeatherAsync(cancellationToken);
            _lastWeatherDisplay = _weatherDisplayBuilder.Build(weatherResult);
            return new WeatherRefreshResult(_lastWeatherDisplay, weatherResult.Status == WeatherLoadStatus.Success);
        }
        finally
        {
            _isRefreshingWeather = false;
        }
    }

    public async Task<CalendarDisplayState> RefreshCalendarAsync(
        CalendarInteractionMode interactionMode,
        CancellationToken cancellationToken)
    {
        if (_isRefreshingCalendar)
        {
            return _lastCalendarDisplay;
        }

        _isRefreshingCalendar = true;

        try
        {
            var loadResult = await _calendarService.GetUpcomingEventsAsync(interactionMode, cancellationToken);
            _lastCalendarDisplay = _calendarDisplayBuilder.Build(
                loadResult,
                _googleCalendarSettings,
                _clockService.Now);
            return _lastCalendarDisplay;
        }
        finally
        {
            _isRefreshingCalendar = false;
        }
    }

    public async Task<CalendarDisplayState> ForgetCalendarAuthorizationAsync(CancellationToken cancellationToken)
    {
        await _calendarService.ForgetAuthorizationAsync(cancellationToken);
        _lastCalendarDisplay = _calendarDisplayBuilder.BuildSignedOutState(_googleCalendarSettings);
        return _lastCalendarDisplay;
    }

    public void SavePlacement(WidgetPlacement placement)
    {
        _placementStore.SaveWindowPlacement(placement.WithUnit(WidgetPlacement.PixelUnit));
    }

    public bool TryLoadPlacement(out WidgetPlacement placement) =>
        _placementStore.TryLoadWindowPlacement(out placement);

    private async Task<WeatherLoadResult> LoadWeatherAsync(CancellationToken cancellationToken)
    {
        if (!await TryEnsureWeatherCoordinatesAsync(cancellationToken))
        {
            return WeatherLoadResult.FromStatus(WeatherLoadStatus.LocationUnavailable);
        }

        try
        {
            var weather = await _weatherService.GetCurrentWeatherAsync(_weatherCoordinates!, cancellationToken);
            return WeatherLoadResult.Success(weather);
        }
        catch
        {
            return WeatherLoadResult.FromStatus(WeatherLoadStatus.Unavailable);
        }
    }

    private async Task<bool> TryEnsureWeatherCoordinatesAsync(CancellationToken cancellationToken)
    {
        if (_weatherCoordinates is not null)
        {
            return true;
        }

        _weatherCoordinates = await _locationService.TryGetCoordinatesAsync(cancellationToken);
        return _weatherCoordinates is not null;
    }
}
