using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;

using TimeWidget.Application.Calendar;
using TimeWidget.Application.Clock;
using TimeWidget.Application.Weather;
using TimeWidget.Application.Widget;
using TimeWidget.Domain.Widget;

namespace TimeWidget.ViewModels;

public sealed class MainWindowViewModel : ObservableObject, IDisposable
{
    private static readonly TimeSpan ClockRefreshInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan WeatherRefreshInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan WeatherResumeRetryDelay = TimeSpan.FromSeconds(10);

    private readonly WidgetDashboardService _dashboardService;
    private readonly ReadOnlyObservableCollection<CityClockItemViewModel> _leftCityTimes;
    private readonly ReadOnlyObservableCollection<CityClockItemViewModel> _rightCityTimes;
    private readonly ObservableCollection<CityClockItemViewModel> _leftCityTimesSource = [];
    private readonly ObservableCollection<CityClockItemViewModel> _rightCityTimesSource = [];
    private readonly ObservableCollection<CalendarEventItemViewModel> _calendarEventsSource = [];
    private readonly ReadOnlyObservableCollection<CalendarEventItemViewModel> _calendarEvents;
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _calendarTimer;
    private readonly DispatcherTimer _weatherTimer;

    private bool _isWallpaperMode = true;
    private string _timeText = string.Empty;
    private string _dateText = string.Empty;
    private string _calendarStatusText = string.Empty;
    private string _weatherTemperatureText = string.Empty;
    private string _weatherConditionText = string.Empty;
    private string _weatherLocationText = "Locating...";
    private bool _hasWeatherDetails;
    private bool _showCompactCalendarSection;
    private bool _showFullCalendarSection;
    private bool _showCalendarEvents;
    private bool _showCalendarStatus;

    public MainWindowViewModel(WidgetDashboardService dashboardService)
    {
        ArgumentNullException.ThrowIfNull(dashboardService);

        _dashboardService = dashboardService;
        _leftCityTimes = new ReadOnlyObservableCollection<CityClockItemViewModel>(_leftCityTimesSource);
        _rightCityTimes = new ReadOnlyObservableCollection<CityClockItemViewModel>(_rightCityTimesSource);
        _calendarEvents = new ReadOnlyObservableCollection<CalendarEventItemViewModel>(_calendarEventsSource);

        ShowForEditingCommand = new RelayCommand(RequestShowForEditing);
        ReturnToWallpaperModeCommand = new RelayCommand(RequestReturnToWallpaperMode);
        CenterUpWidgetCommand = new RelayCommand(RequestCenterUpWidget);

        _clockTimer = new DispatcherTimer
        {
            Interval = ClockRefreshInterval
        };
        _calendarTimer = new DispatcherTimer
        {
            Interval = _dashboardService.CalendarRefreshInterval
        };
        _weatherTimer = new DispatcherTimer
        {
            Interval = WeatherRefreshInterval
        };

        _clockTimer.Tick += (_, _) => UpdateTime();
        _calendarTimer.Tick += async (_, _) => await RefreshCalendarAsync(CalendarInteractionMode.Background);
        _weatherTimer.Tick += async (_, _) => await RefreshWeatherCoreAsync();

        UpdateTime();
        _clockTimer.Start();
    }

    public event EventHandler? ShowForEditingRequested;

    public event EventHandler? ReturnToWallpaperModeRequested;

    public event EventHandler? CenterUpWidgetRequested;

    public ICommand ShowForEditingCommand { get; }

    public ICommand ReturnToWallpaperModeCommand { get; }

    public ICommand CenterUpWidgetCommand { get; }

    public bool IsWallpaperMode
    {
        get => _isWallpaperMode;
        private set
        {
            if (SetProperty(ref _isWallpaperMode, value))
            {
                OnPropertyChanged(nameof(EditChromeVisible));
            }
        }
    }

    public bool EditChromeVisible => !IsWallpaperMode;

    public ReadOnlyObservableCollection<CityClockItemViewModel> LeftCityTimes => _leftCityTimes;

    public ReadOnlyObservableCollection<CityClockItemViewModel> RightCityTimes => _rightCityTimes;

    public bool HasLeftCityTimes => LeftCityTimes.Count > 0;

    public bool HasRightCityTimes => RightCityTimes.Count > 0;

    public ReadOnlyObservableCollection<CalendarEventItemViewModel> CalendarEvents => _calendarEvents;

    public string TimeText
    {
        get => _timeText;
        private set => SetProperty(ref _timeText, value);
    }

    public string DateText
    {
        get => _dateText;
        private set => SetProperty(ref _dateText, value);
    }

    public string CalendarStatusText
    {
        get => _calendarStatusText;
        private set => SetProperty(ref _calendarStatusText, value);
    }

    public string WeatherTemperatureText
    {
        get => _weatherTemperatureText;
        private set => SetProperty(ref _weatherTemperatureText, value);
    }

    public string WeatherConditionText
    {
        get => _weatherConditionText;
        private set => SetProperty(ref _weatherConditionText, value);
    }

    public string WeatherLocationText
    {
        get => _weatherLocationText;
        private set => SetProperty(ref _weatherLocationText, value);
    }

    public bool HasWeatherDetails
    {
        get => _hasWeatherDetails;
        private set => SetProperty(ref _hasWeatherDetails, value);
    }

    public bool ShowCompactCalendarSection
    {
        get => _showCompactCalendarSection;
        private set => SetProperty(ref _showCompactCalendarSection, value);
    }

    public bool ShowFullCalendarSection
    {
        get => _showFullCalendarSection;
        private set => SetProperty(ref _showFullCalendarSection, value);
    }

    public bool ShowCalendarEvents
    {
        get => _showCalendarEvents;
        private set => SetProperty(ref _showCalendarEvents, value);
    }

    public bool ShowCalendarStatus
    {
        get => _showCalendarStatus;
        private set => SetProperty(ref _showCalendarStatus, value);
    }

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

    public void HandleSuspend()
    {
        _calendarTimer.Stop();
        _weatherTimer.Stop();
    }

    public async Task HandleResumeAsync()
    {
        _calendarTimer.Stop();
        _weatherTimer.Stop();

        var weatherRefresh = await _dashboardService.RefreshWeatherAsync(CancellationToken.None);
        ApplyWeatherState(weatherRefresh.DisplayState);
        if (!weatherRefresh.Succeeded)
        {
            await Task.Delay(WeatherResumeRetryDelay);
            await RefreshWeatherCoreAsync();
        }

        _weatherTimer.Start();

        await RefreshCalendarAsync(CalendarInteractionMode.Background);
        if (_dashboardService.IsCalendarEnabled)
        {
            _calendarTimer.Start();
        }
    }

    public async Task RefreshWeatherNowAsync()
    {
        _weatherTimer.Stop();
        await RefreshWeatherCoreAsync();
        _weatherTimer.Start();
    }

    public async Task RefreshCalendarNowAsync()
    {
        _calendarTimer.Stop();
        await RefreshCalendarAsync(CalendarInteractionMode.Interactive);

        if (_dashboardService.IsCalendarEnabled)
        {
            _calendarTimer.Start();
        }
    }

    public async Task ForgetCalendarAuthorizationAsync()
    {
        _calendarTimer.Stop();
        ApplyCalendarState(await _dashboardService.ForgetCalendarAuthorizationAsync(CancellationToken.None));

        if (_dashboardService.IsCalendarEnabled)
        {
            _calendarTimer.Start();
        }
    }

    public void SaveScreenPosition(int left, int top)
    {
        _dashboardService.SavePlacement(new WidgetPlacement(left, top));
    }

    public bool TryLoadWindowPlacement(out WidgetPlacement placement)
    {
        return _dashboardService.TryLoadPlacement(out placement);
    }

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

    private void RequestCenterUpWidget()
    {
        CenterUpWidgetRequested?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateTime()
    {
        ApplyClockState(_dashboardService.GetClockDisplayState());
    }

    private async Task RefreshWeatherCoreAsync()
    {
        var weatherRefresh = await _dashboardService.RefreshWeatherAsync(CancellationToken.None);
        ApplyWeatherState(weatherRefresh.DisplayState);
    }

    private async Task RefreshCalendarAsync(CalendarInteractionMode interactionMode)
    {
        ApplyCalendarState(await _dashboardService.RefreshCalendarAsync(interactionMode, CancellationToken.None));
    }

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
}
