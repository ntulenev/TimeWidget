using System.Drawing;
using System.IO;
using System.Windows;

using TimeWidget.Abstractions;
using TimeWidget.Infrastructure;
using TimeWidget.Models;
using TimeWidget.ViewModels;
using TimeWidget.Views;

using Forms = System.Windows.Forms;

namespace TimeWidget;

public partial class App : System.Windows.Application
{
    private Forms.NotifyIcon? _notifyIcon;
    private MainWindowViewModel? _mainWindowViewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mainWindowViewModel = CreateMainWindowViewModel();
        var widgetPositioningSettings = CreateWidgetPositioningSettings();
        var mainWindow = new MainWindow(_mainWindowViewModel, widgetPositioningSettings);
        MainWindow = mainWindow;
        ConfigureTray(mainWindow, _mainWindowViewModel);
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        base.OnExit(e);
    }

    private static MainWindowViewModel CreateMainWindowViewModel()
    {
        IClockService clockService = new SystemClockService();
        ILocationService locationService = new WindowsLocationService();
        IWeatherService weatherService = new OpenMeteoWeatherService();
        IWidgetSettingsStore settingsStore = new JsonWidgetSettingsStore();
        IClockCitiesSettingsProvider clockCitiesSettingsProvider = new JsonAppSettingsClockCitiesSettingsProvider();

        return new MainWindowViewModel(
            clockService,
            locationService,
            weatherService,
            settingsStore,
            clockCitiesSettingsProvider);
    }

    private static WidgetPositioningSettings CreateWidgetPositioningSettings()
    {
        IWidgetPositioningSettingsProvider widgetPositioningSettingsProvider =
            new JsonAppSettingsWidgetPositioningSettingsProvider();

        return widgetPositioningSettingsProvider.Load();
    }

    private void ConfigureTray(MainWindow mainWindow, MainWindowViewModel viewModel)
    {
        var contextMenu = new Forms.ContextMenuStrip();

        var showForSetupItem = new Forms.ToolStripMenuItem("Show for setup");
        showForSetupItem.Click += (_, _) => viewModel.ShowForEditingCommand.Execute(null);

        var wallpaperModeItem = new Forms.ToolStripMenuItem("Back to wallpaper");
        wallpaperModeItem.Click += (_, _) => viewModel.ReturnToWallpaperModeCommand.Execute(null);

        var centerUpWidgetItem = new Forms.ToolStripMenuItem("Center widget");
        centerUpWidgetItem.Click += (_, _) => viewModel.CenterUpWidgetCommand.Execute(null);

        var exitItem = new Forms.ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => mainWindow.Close();

        contextMenu.Items.Add(showForSetupItem);
        contextMenu.Items.Add(wallpaperModeItem);
        contextMenu.Items.Add(centerUpWidgetItem);
        contextMenu.Items.Add(new Forms.ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        var trayIconPath = Path.Combine(AppContext.BaseDirectory, "clock_icon-icons.com_54407.ico");
        var trayIcon = File.Exists(trayIconPath)
            ? new Icon(trayIconPath)
            : SystemIcons.Application;

        _notifyIcon = new Forms.NotifyIcon
        {
            ContextMenuStrip = contextMenu,
            Icon = trayIcon,
            Text = "Time Widget",
            Visible = true
        };

        _notifyIcon.DoubleClick += (_, _) => viewModel.ShowForEditingCommand.Execute(null);
    }
}
