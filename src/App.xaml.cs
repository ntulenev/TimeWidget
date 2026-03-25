using System.Drawing;
using System.IO;
using System.Windows;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using TimeWidget.ViewModels;
using TimeWidget.Views;

using Forms = System.Windows.Forms;

namespace TimeWidget;

public partial class App : System.Windows.Application
{
    private Forms.NotifyIcon? _notifyIcon;
    private ServiceProvider? _serviceProvider;
    private IServiceScope? _mainWindowScope;
    private IServiceScopeFactory? _scopeFactory;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _serviceProvider = CreateServiceProvider();
        _scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var mainWindow = CreateMainWindow();
        var mainWindowViewModel = _mainWindowScope!.ServiceProvider.GetRequiredService<MainWindowViewModel>();
        MainWindow = mainWindow;
        ConfigureTray(mainWindow, mainWindowViewModel);
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        DisposeMainWindowScope();
        _serviceProvider?.Dispose();

        base.OnExit(e);
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddTimeWidgetInfrastructure(configuration);
        services.AddTimeWidgetPresentation();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }

    private MainWindow CreateMainWindow()
    {
        ArgumentNullException.ThrowIfNull(_scopeFactory);

        _mainWindowScope = _scopeFactory.CreateScope();

        var mainWindow = _mainWindowScope.ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Closed += MainWindow_Closed;
        return mainWindow;
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        if (sender is MainWindow mainWindow)
        {
            mainWindow.Closed -= MainWindow_Closed;
        }

        DisposeMainWindowScope();
    }

    private void DisposeMainWindowScope()
    {
        _mainWindowScope?.Dispose();
        _mainWindowScope = null;
    }

    private void ConfigureTray(MainWindow mainWindow, MainWindowViewModel viewModel)
    {
        var contextMenu = new Forms.ContextMenuStrip();

        var showForSetupItem = new Forms.ToolStripMenuItem("Show for setup");
        showForSetupItem.Click += (_, _) => viewModel.ShowForEditingCommand.Execute(null);

        var wallpaperModeItem = new Forms.ToolStripMenuItem("Back to wallpaper");
        wallpaperModeItem.Click += (_, _) => viewModel.ReturnToWallpaperModeCommand.Execute(null);

        var refreshWeatherItem = new Forms.ToolStripMenuItem("Refresh weather");
        refreshWeatherItem.Click += async (_, _) => await viewModel.RefreshWeatherNowAsync();

        var refreshCalendarItem = new Forms.ToolStripMenuItem("Refresh calendar");
        refreshCalendarItem.Click += async (_, _) => await viewModel.RefreshCalendarNowAsync();

        var forgetCalendarItem = new Forms.ToolStripMenuItem("Forget Google Calendar sign-in");
        forgetCalendarItem.Click += async (_, _) => await viewModel.ForgetCalendarAuthorizationAsync();

        var centerUpWidgetItem = new Forms.ToolStripMenuItem("Center widget");
        centerUpWidgetItem.Click += (_, _) =>
        {
            if (GetOrderedScreens().Length <= 1)
            {
                viewModel.CenterUpWidgetCommand.Execute(null);
            }
        };
        centerUpWidgetItem.DropDownOpening += (_, _) =>
            ConfigureCenterWidgetMenu(centerUpWidgetItem, mainWindow);
        ConfigureCenterWidgetMenu(centerUpWidgetItem, mainWindow);

        var exitItem = new Forms.ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => mainWindow.Close();

        contextMenu.Items.Add(showForSetupItem);
        contextMenu.Items.Add(wallpaperModeItem);
        contextMenu.Items.Add(refreshWeatherItem);
        contextMenu.Items.Add(refreshCalendarItem);
        contextMenu.Items.Add(forgetCalendarItem);
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

    private static void ConfigureCenterWidgetMenu(
        Forms.ToolStripMenuItem centerUpWidgetItem,
        MainWindow mainWindow)
    {
        centerUpWidgetItem.DropDownItems.Clear();

        var screens = GetOrderedScreens();
        if (screens.Length <= 1)
        {
            centerUpWidgetItem.Text = "Center widget";
            centerUpWidgetItem.Enabled = true;
            return;
        }

        centerUpWidgetItem.Text = "Center on monitor";

        foreach (var screen in screens)
        {
            var screenIndex = GetDisplayIndex(screen);
            var itemText = screenIndex > 0
                ? $"Monitor {screenIndex}"
                : screen.DeviceName;

            var screenItem = new Forms.ToolStripMenuItem(itemText);
            screenItem.Click += (_, _) => mainWindow.CenterUpOnScreen(screen);
            centerUpWidgetItem.DropDownItems.Add(screenItem);
        }
    }

    private static Forms.Screen[] GetOrderedScreens()
    {
        return Forms.Screen.AllScreens
            .OrderBy(screen => GetDisplayIndex(screen) is var index && index > 0 ? index : int.MaxValue)
            .ThenBy(screen => screen.DeviceName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static int GetDisplayIndex(Forms.Screen screen)
    {
        var digits = new string(screen.DeviceName.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var index) ? index : -1;
    }
}
