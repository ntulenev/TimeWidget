using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;

using TimeWidget.Application.Calendar;
using TimeWidget.Application.Clock;
using TimeWidget.Application.Weather;
using TimeWidget.Application.Widget;
using TimeWidget.Domain.Widget;

namespace TimeWidget.ViewModels;

/// <summary>
/// Exposes the state and commands for the main widget window.
/// </summary>
public sealed class MainWindowViewModel : ObservableObject, IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="dashboardService">The dashboard service used to load widget data.</param>
    public MainWindowViewModel(WidgetDashboardService dashboardService)
    {
        ArgumentNullException.ThrowIfNull(dashboardService);

        _dashboardService = dashboardService;
        LeftCityTimes = new ReadOnlyObservableCollection<CityClockItemViewModel>(_leftCityTimesSource);
        RightCityTimes = new ReadOnlyObservableCollection<CityClockItemViewModel>(_rightCityTimesSource);
        CalendarEvents = new ReadOnlyObservableCollection<CalendarEventItemViewModel>(_calendarEventsSource);

        ShowForEditingCommand = new RelayCommand(RequestShowForEditing);
        ReturnToWallpaperModeCommand = new RelayCommand(RequestReturnToWallpaperMode);
        CenterUpWidgetCommand = new RelayCommand(RequestCenterUpWidget);

        _clockTimer = new DispatcherTimer
        {
            Interval = _clockRefreshInterval
        };
        _calendarTimer = new DispatcherTimer
        {
            Interval = _dashboardService.CalendarRefreshInterval
        };
        _weatherTimer = new DispatcherTimer
        {
            Interval = _weatherRefreshInterval
        };

        _clockTimer.Tick += (_, _) => UpdateTime();
        _calendarTimer.Tick += async (_, _) => await RefreshCalendarAsync(CalendarInteractionMode.Background);
        _weatherTimer.Tick += async (_, _) => await RefreshWeatherCoreAsync();

        UpdateTime();
        _clockTimer.Start();
    }

    /// <summary>
    /// Occurs when the window should switch into editing mode.
    /// </summary>
    public event EventHandler? ShowForEditingRequested;

    /// <summary>
    /// Occurs when the window should return to wallpaper mode.
    /// </summary>
    public event EventHandler? ReturnToWallpaperModeRequested;

    /// <summary>
    /// Occurs when the widget should be centered on the current screen.
    /// </summary>
    public event EventHandler? CenterUpWidgetRequested;

    /// <summary>
    /// Gets the command that switches the widget into editing mode.
    /// </summary>
    public ICommand ShowForEditingCommand { get; }

    /// <summary>
    /// Gets the command that returns the widget to wallpaper mode.
    /// </summary>
    public ICommand ReturnToWallpaperModeCommand { get; }

    /// <summary>
    /// Gets the command that centers the widget.
    /// </summary>
    public ICommand CenterUpWidgetCommand { get; }

    /// <summary>
    /// Gets a value indicating whether the widget is currently in wallpaper mode.
    /// </summary>
    public bool IsWallpaperMode {
        get; private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(EditChromeVisible));
            }
        }
    } = true;

    /// <summary>
    /// Gets a value indicating whether edit chrome should be visible.
    /// </summary>
    public bool EditChromeVisible => !IsWallpaperMode;

    /// <summary>
    /// Gets the left-side city clocks.
    /// </summary>
    public ReadOnlyObservableCollection<CityClockItemViewModel> LeftCityTimes { get; }

    /// <summary>
    /// Gets the right-side city clocks.
    /// </summary>
    public ReadOnlyObservableCollection<CityClockItemViewModel> RightCityTimes { get; }

    /// <summary>
    /// Gets a value indicating whether the left-side clocks collection has items.
    /// </summary>
    public bool HasLeftCityTimes => LeftCityTimes.Count > 0;

    /// <summary>
    /// Gets a value indicating whether the right-side clocks collection has items.
    /// </summary>
    public bool HasRightCityTimes => RightCityTimes.Count > 0;

    /// <summary>
    /// Gets the calendar events shown in the widget.
    /// </summary>
    public ReadOnlyObservableCollection<CalendarEventItemViewModel> CalendarEvents { get; }

    /// <summary>
    /// Gets the main time text.
    /// </summary>
    public string TimeText { get; private set => SetProperty(ref field, value); } = string.Empty;

    /// <summary>
    /// Gets the formatted date text.
    /// </summary>
    public string DateText { get; private set => SetProperty(ref field, value); } = string.Empty;

    /// <summary>
    /// Gets the calendar status text.
    /// </summary>
    public string CalendarStatusText { get; private set => SetProperty(ref field, value); } = string.Empty;

    /// <summary>
    /// Gets the weather temperature text.
    /// </summary>
    public string WeatherTemperatureText { get; private set => SetProperty(ref field, value); } = string.Empty;

    /// <summary>
    /// Gets the weather condition text.
    /// </summary>
    public string WeatherConditionText { get; private set => SetProperty(ref field, value); } = string.Empty;

    /// <summary>
    /// Gets the weather location text.
    /// </summary>
    public string WeatherLocationText { get; private set => SetProperty(ref field, value); } = "Locating...";

    /// <summary>
    /// Gets a value indicating whether weather details are available.
    /// </summary>
    public bool HasWeatherDetails { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets a value indicating whether the compact calendar section should be shown.
    /// </summary>
    public bool ShowCompactCalendarSection { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets a value indicating whether the full calendar section should be shown.
    /// </summary>
    public bool ShowFullCalendarSection { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets a value indicating whether calendar events should be shown.
    /// </summary>
    public bool ShowCalendarEvents { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets a value indicating whether the calendar status should be shown.
    /// </summary>
    public bool ShowCalendarStatus { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// Initializes the widget data and starts background refresh timers.
    /// </summary>
    /// <returns>A task that completes when initialization finishes.</returns>
    public async Task InitializeAsync()
    {
        await RefreshWeatherCoreAsync();
        _weatherTimer.Start();

        await RefreshCalendarAsync(CalendarInteractionMode.Background);
        if (_dashboardService.IsCalendarEnabled)
        {
            _calendarTimer.Start();
        }
    }

    /// <summary>
    /// Stops background refresh timers when the system is suspending.
    /// </summary>
    public void HandleSuspend()
    {
        _calendarTimer.Stop();
        _weatherTimer.Stop();
    }

    /// <summary>
    /// Restarts background refreshes after the system resumes.
    /// </summary>
    /// <returns>A task that completes when resume handling finishes.</returns>
    public async Task HandleResumeAsync()
    {
        _calendarTimer.Stop();
        _weatherTimer.Stop();

        var weatherRefresh = await _dashboardService.RefreshWeatherAsync(CancellationToken.None);
        ApplyWeatherState(weatherRefresh.DisplayState);
        if (!weatherRefresh.Succeeded)
        {
            await Task.Delay(_weatherResumeRetryDelay);
            await RefreshWeatherCoreAsync();
        }

        _weatherTimer.Start();

        await RefreshCalendarAsync(CalendarInteractionMode.Background);
        if (_dashboardService.IsCalendarEnabled)
        {
            _calendarTimer.Start();
        }
    }

    /// <summary>
    /// Refreshes weather data immediately.
    /// </summary>
    /// <returns>A task that completes when the refresh finishes.</returns>
    public async Task RefreshWeatherNowAsync()
    {
        _weatherTimer.Stop();
        await RefreshWeatherCoreAsync();
        _weatherTimer.Start();
    }

    /// <summary>
    /// Refreshes calendar data immediately.
    /// </summary>
    /// <returns>A task that completes when the refresh finishes.</returns>
    public async Task RefreshCalendarNowAsync()
    {
        _calendarTimer.Stop();
        await RefreshCalendarAsync(CalendarInteractionMode.Interactive);

        if (_dashboardService.IsCalendarEnabled)
        {
            _calendarTimer.Start();
        }
    }

    /// <summary>
    /// Removes calendar authorization and updates the view state.
    /// </summary>
    /// <returns>A task that completes when the operation finishes.</returns>
    public async Task ForgetCalendarAuthorizationAsync()
    {
        _calendarTimer.Stop();
        ApplyCalendarState(await _dashboardService.ForgetCalendarAuthorizationAsync(CancellationToken.None));

        if (_dashboardService.IsCalendarEnabled)
        {
            _calendarTimer.Start();
        }
    }

    /// <summary>
    /// Saves the current screen position.
    /// </summary>
    /// <param name="left">The left position in screen pixels.</param>
    /// <param name="top">The top position in screen pixels.</param>
    public void SaveScreenPosition(int left, int top) => _dashboardService.SavePlacement(new WidgetPlacement(left, top));

    /// <summary>
    /// Attempts to load the previously saved window placement.
    /// </summary>
    /// <param name="placement">When this method returns, contains the loaded placement if one exists.</param>
    /// <returns><see langword="true"/> when a placement was loaded; otherwise, <see langword="false"/>.</returns>
    public bool TryLoadWindowPlacement(out WidgetPlacement placement) => _dashboardService.TryLoadPlacement(out placement);

    /// <summary>
    /// Stops background timers owned by the view model.
    /// </summary>
    public void Dispose()
    {
        _clockTimer.Stop();
        _calendarTimer.Stop();
        _weatherTimer.Stop();
    }

    private void RequestShowForEditing()
    {
        IsWallpaperMode = false;
        ShowForEditingRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RequestReturnToWallpaperMode()
    {
        IsWallpaperMode = true;
        ReturnToWallpaperModeRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RequestCenterUpWidget() => CenterUpWidgetRequested?.Invoke(this, EventArgs.Empty);

    private void UpdateTime() => ApplyClockState(_dashboardService.GetClockDisplayState());

    private async Task RefreshWeatherCoreAsync()
    {
        var weatherRefresh = await _dashboardService.RefreshWeatherAsync(CancellationToken.None);
        ApplyWeatherState(weatherRefresh.DisplayState);
    }

    private async Task RefreshCalendarAsync(CalendarInteractionMode interactionMode) => ApplyCalendarState(await _dashboardService.RefreshCalendarAsync(interactionMode, CancellationToken.None));

    private void ApplyClockState(ClockDisplayState state)
    {
        TimeText = state.TimeText;
        DateText = state.DateText;
        ApplyCityCollection(_leftCityTimesSource, state.LeftCityTimes);
        ApplyCityCollection(_rightCityTimesSource, state.RightCityTimes);
        OnPropertyChanged(nameof(HasLeftCityTimes));
        OnPropertyChanged(nameof(HasRightCityTimes));
    }

    private void ApplyWeatherState(WeatherDisplayState state)
    {
        WeatherTemperatureText = state.TemperatureText;
        WeatherConditionText = state.ConditionText;
        WeatherLocationText = state.LocationText;
        HasWeatherDetails = state.HasDetails;
    }

    private void ApplyCalendarState(CalendarDisplayState state)
    {
        _calendarEventsSource.Clear();
        foreach (var calendarEvent in state.Events)
        {
            _calendarEventsSource.Add(new CalendarEventItemViewModel(
                calendarEvent.Title,
                calendarEvent.ScheduleText,
                calendarEvent.ResponseSymbol));
        }

        CalendarStatusText = state.StatusText;
        ShowCalendarEvents = state.ShowEvents;
        ShowCalendarStatus = state.ShowStatus;
        ShowCompactCalendarSection = state.ShowCompactSection;
        ShowFullCalendarSection = state.ShowFullSection;
    }

    private static void ApplyCityCollection(
        ObservableCollection<CityClockItemViewModel> target,
        IReadOnlyList<CityClockDisplayState> source)
    {
        while (target.Count < source.Count)
        {
            target.Add(new CityClockItemViewModel());
        }

        while (target.Count > source.Count)
        {
            target.RemoveAt(target.Count - 1);
        }

        for (var index = 0; index < source.Count; index++)
        {
            target[index].Apply(source[index]);
        }
    }

    private static readonly TimeSpan _clockRefreshInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan _weatherRefreshInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan _weatherResumeRetryDelay = TimeSpan.FromSeconds(10);

    private readonly WidgetDashboardService _dashboardService;
    private readonly ObservableCollection<CityClockItemViewModel> _leftCityTimesSource = [];
    private readonly ObservableCollection<CityClockItemViewModel> _rightCityTimesSource = [];
    private readonly ObservableCollection<CalendarEventItemViewModel> _calendarEventsSource = [];
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _calendarTimer;
    private readonly DispatcherTimer _weatherTimer;
}
