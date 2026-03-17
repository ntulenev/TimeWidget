using System.Globalization;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using TimeWidget.Abstractions;
using TimeWidget.Models;

namespace TimeWidget.ViewModels;

public sealed class MainWindowViewModel : ObservableObject, IDisposable
{
    private static readonly TimeSpan ClockRefreshInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan WeatherRefreshInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan WeatherResumeRetryDelay = TimeSpan.FromSeconds(10);
    private static readonly bool Use24HourClock = true;
    private static readonly CultureInfo DateCulture = CultureInfo.GetCultureInfo("en-US");

    private readonly IClockService _clockService;
    private readonly ILocationService _locationService;
    private readonly IWeatherService _weatherService;
    private readonly IWidgetSettingsStore _settingsStore;
    private readonly ReadOnlyObservableCollection<CityClockItemViewModel> _leftCityTimes;
    private readonly ReadOnlyObservableCollection<CityClockItemViewModel> _rightCityTimes;
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _weatherTimer;

    private Coordinates? _weatherCoordinates;
    private bool _isRefreshingWeather;
    private bool _isWallpaperMode = true;
    private string _timeText = string.Empty;
    private string _dateText = string.Empty;
    private string _weatherTemperatureText = string.Empty;
    private string _weatherConditionText = string.Empty;
    private string _weatherLocationText = "Locating...";
    private Visibility _weatherDetailsVisibility = Visibility.Collapsed;

    public MainWindowViewModel(
        IClockService clockService,
        ILocationService locationService,
        IWeatherService weatherService,
        IWidgetSettingsStore settingsStore,
        IClockCitiesSettingsProvider clockCitiesSettingsProvider)
    {
        ArgumentNullException.ThrowIfNull(clockService);
        ArgumentNullException.ThrowIfNull(locationService);
        ArgumentNullException.ThrowIfNull(weatherService);
        ArgumentNullException.ThrowIfNull(settingsStore);
        ArgumentNullException.ThrowIfNull(clockCitiesSettingsProvider);

        _clockService = clockService;
        _locationService = locationService;
        _weatherService = weatherService;
        _settingsStore = settingsStore;

        var clockCitiesSettings = clockCitiesSettingsProvider.Load();
        _leftCityTimes = new ReadOnlyObservableCollection<CityClockItemViewModel>(
            new ObservableCollection<CityClockItemViewModel>(
                CreateCityClockItems(clockCitiesSettings.LeftCities)));
        _rightCityTimes = new ReadOnlyObservableCollection<CityClockItemViewModel>(
            new ObservableCollection<CityClockItemViewModel>(
                CreateCityClockItems(clockCitiesSettings.RightCities)));

        ShowForEditingCommand = new RelayCommand(RequestShowForEditing);
        ReturnToWallpaperModeCommand = new RelayCommand(RequestReturnToWallpaperMode);
        CenterUpWidgetCommand = new RelayCommand(RequestCenterUpWidget);

        _clockTimer = new DispatcherTimer
        {
            Interval = ClockRefreshInterval
        };

        _weatherTimer = new DispatcherTimer
        {
            Interval = WeatherRefreshInterval
        };

        _clockTimer.Tick += (_, _) => UpdateTime();
        _weatherTimer.Tick += async (_, _) => await RefreshWeatherAsync(CancellationToken.None);

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
                OnPropertyChanged(nameof(EditChromeVisibility));
            }
        }
    }

    public Visibility EditChromeVisibility =>
        IsWallpaperMode ? Visibility.Collapsed : Visibility.Visible;

    public ReadOnlyObservableCollection<CityClockItemViewModel> LeftCityTimes => _leftCityTimes;

    public ReadOnlyObservableCollection<CityClockItemViewModel> RightCityTimes => _rightCityTimes;

    public Visibility LeftCityTimesVisibility =>
        LeftCityTimes.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility RightCityTimesVisibility =>
        RightCityTimes.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

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

    public Visibility WeatherDetailsVisibility
    {
        get => _weatherDetailsVisibility;
        private set => SetProperty(ref _weatherDetailsVisibility, value);
    }

    public async Task InitializeAsync()
    {
        _weatherCoordinates = await _locationService.TryGetCoordinatesAsync(CancellationToken.None);

        if (_weatherCoordinates is null)
        {
            SetWeatherStatus("Enable Windows location");
            return;
        }

        await RefreshWeatherAsync(CancellationToken.None);
        _weatherTimer.Start();
    }

    public void HandleSuspend()
    {
        _weatherTimer.Stop();
    }

    public async Task HandleResumeAsync()
    {
        _weatherTimer.Stop();

        if (_weatherCoordinates is null)
        {
            _weatherCoordinates = await _locationService.TryGetCoordinatesAsync(CancellationToken.None);
        }

        if (_weatherCoordinates is null)
        {
            SetWeatherStatus("Enable Windows location");
            return;
        }

        var refreshed = await RefreshWeatherAsync(CancellationToken.None);
        if (!refreshed)
        {
            await Task.Delay(WeatherResumeRetryDelay);
            await RefreshWeatherAsync(CancellationToken.None);
        }

        _weatherTimer.Start();
    }

    public void SaveScreenPosition(int left, int top)
    {
        _settingsStore.SaveWindowPlacement(new WidgetPlacementSettings(left, top, "px"));
    }

    public bool TryLoadWindowPlacement(out WidgetPlacementSettings placement)
    {
        return _settingsStore.TryLoadWindowPlacement(out placement);
    }

    public void Dispose()
    {
        _clockTimer.Stop();
        _weatherTimer.Stop();

        if (_weatherService is IDisposable disposableWeatherService)
        {
            disposableWeatherService.Dispose();
        }
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
        var now = _clockService.Now;
        var currentCulture = CultureInfo.CurrentCulture;
        var dateText = now.ToString("dddd, d MMMM yyyy", DateCulture);

        TimeText = now.ToString(Use24HourClock ? "HH:mm" : "hh:mm", currentCulture);
        DateText = string.IsNullOrEmpty(dateText)
            ? string.Empty
            : char.ToUpper(dateText[0], DateCulture) + dateText[1..];

        foreach (var cityTime in LeftCityTimes)
        {
            cityTime.Update(now, currentCulture, Use24HourClock);
        }

        foreach (var cityTime in RightCityTimes)
        {
            cityTime.Update(now, currentCulture, Use24HourClock);
        }
    }

    private async Task<bool> RefreshWeatherAsync(CancellationToken cancellationToken)
    {
        if (_isRefreshingWeather || _weatherCoordinates is null)
        {
            return false;
        }

        _isRefreshingWeather = true;

        try
        {
            var weather = await _weatherService.GetCurrentWeatherAsync(
                _weatherCoordinates,
                cancellationToken);
            ApplyWeather(weather);
            return true;
        }
        catch
        {
            SetWeatherStatus("Weather unavailable");
            return false;
        }
        finally
        {
            _isRefreshingWeather = false;
        }
    }

    private void ApplyWeather(WeatherInfo weather)
    {
        WeatherTemperatureText = $"{weather.Temperature}\u00B0";
        WeatherConditionText = weather.Condition;
        WeatherLocationText = weather.Location;
        WeatherDetailsVisibility = Visibility.Visible;
    }

    private void SetWeatherStatus(string message)
    {
        WeatherTemperatureText = string.Empty;
        WeatherConditionText = string.Empty;
        WeatherLocationText = message;
        WeatherDetailsVisibility = Visibility.Collapsed;
    }

    private static IEnumerable<CityClockItemViewModel> CreateCityClockItems(
        IEnumerable<CityClockSettings> cities)
    {
        foreach (var city in cities)
        {
            yield return new CityClockItemViewModel(city.Name, city.TimeZoneId);
        }
    }
}
