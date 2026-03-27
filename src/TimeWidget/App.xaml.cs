using System.Drawing;
using System.IO;
using System.Windows;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using TimeWidget.Application.Widget;
using TimeWidget.Infrastructure.Configuration;
using TimeWidget.ViewModels;
using TimeWidget.Views;

using Forms = System.Windows.Forms;

namespace TimeWidget;

public partial class App : System.Windows.Application
{
    private Forms.NotifyIcon? _notifyIcon;
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = CreateHost();
        _host.StartAsync().GetAwaiter().GetResult();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        var mainWindowViewModel = _host.Services.GetRequiredService<MainWindowViewModel>();
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

        if (_host is not null)
        {
            _host.StopAsync().GetAwaiter().GetResult();
            _host.Dispose();
        }

        base.OnExit(e);
    }

    private static IHost CreateHost()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = AppContext.BaseDirectory
        });

        builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
        builder.Services.AddTimeWidgetApplication();
        builder.Services.AddTimeWidgetInfrastructure(builder.Configuration);
        builder.Services.AddTimeWidgetPresentation();

        return builder.Build();
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

        var calendarItem = new Forms.ToolStripMenuItem("Calendar");
        calendarItem.DropDownItems.Add(refreshCalendarItem);
        calendarItem.DropDownItems.Add(forgetCalendarItem);

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
        contextMenu.Items.Add(calendarItem);
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
