using System.Text.Json;

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

/// <summary>
/// Coordinates the application services needed to populate the widget dashboard.
/// </summary>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="WidgetDashboardService"/> class.
    /// </summary>
    /// <param name="clockService">The clock service.</param>
    /// <param name="calendarService">The calendar service.</param>
    /// <param name="locationService">The location service.</param>
    /// <param name="weatherService">The weather service.</param>
    /// <param name="placementStore">The widget placement store.</param>
    /// <param name="clockDisplayBuilder">The clock display builder.</param>
    /// <param name="calendarDisplayBuilder">The calendar display builder.</param>
    /// <param name="weatherDisplayBuilder">The weather display builder.</param>
    /// <param name="clockCitiesOptions">The configured city clocks.</param>
    /// <param name="googleCalendarOptions">The configured Google Calendar options.</param>
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

    /// <summary>
    /// Gets a value indicating whether calendar integration is enabled.
    /// </summary>
    public bool IsCalendarEnabled => _calendarService.IsEnabled;

    /// <summary>
    /// Gets the interval used for background calendar refreshes.
    /// </summary>
    public TimeSpan CalendarRefreshInterval => TimeSpan.FromMinutes(_googleCalendarSettings.RefreshMinutes);

    /// <summary>
    /// Builds the current clock display state.
    /// </summary>
    /// <returns>The clock display state.</returns>
    public ClockDisplayState GetClockDisplayState() =>
        _clockDisplayBuilder.Build(_clockService.Now, _clockCitiesSettings);

    /// <summary>
    /// Refreshes weather information for the widget.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The weather refresh result.</returns>
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

    /// <summary>
    /// Refreshes calendar information for the widget.
    /// </summary>
    /// <param name="interactionMode">The interaction mode that triggered the refresh.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The calendar display state.</returns>
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

    /// <summary>
    /// Removes calendar authorization and returns the resulting signed-out state.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The signed-out calendar state.</returns>
    public async Task<CalendarDisplayState> ForgetCalendarAuthorizationAsync(CancellationToken cancellationToken)
    {
        await _calendarService.ForgetAuthorizationAsync(cancellationToken);
        _lastCalendarDisplay = _calendarDisplayBuilder.BuildSignedOutState(_googleCalendarSettings);
        return _lastCalendarDisplay;
    }

    /// <summary>
    /// Saves the current widget placement.
    /// </summary>
    /// <param name="placement">The placement to persist.</param>
    public void SavePlacement(WidgetPlacement placement)
    {
        _placementStore.SaveWindowPlacement(placement.WithUnit(WidgetPlacement.PixelUnit));
    }

    /// <summary>
    /// Attempts to load the saved widget placement.
    /// </summary>
    /// <param name="placement">When this method returns, contains the loaded placement if one exists.</param>
    /// <returns><see langword="true"/> when a saved placement was loaded; otherwise, <see langword="false"/>.</returns>
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
        catch (HttpRequestException)
        {
            return WeatherLoadResult.FromStatus(WeatherLoadStatus.Unavailable);
        }
        catch (InvalidOperationException)
        {
            return WeatherLoadResult.FromStatus(WeatherLoadStatus.Unavailable);
        }
        catch (JsonException)
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
