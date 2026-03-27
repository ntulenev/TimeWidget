using System.Windows;
using System.Windows.Input;

using TimeWidget.Presentation;
using TimeWidget.ViewModels;

using Forms = System.Windows.Forms;

namespace TimeWidget.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowController _controller;

    public MainWindow(
        MainWindowViewModel viewModel,
        MainWindowController controller)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(controller);

        InitializeComponent();

        DataContext = viewModel;
        _controller = controller;
        _controller.Attach(this, viewModel, RootScaleTransform);

        SourceInitialized += MainWindow_SourceInitialized;
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _controller.OnLoadedAsync();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _controller.OnClosed();
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        _controller.OnSourceInitialized();
    }

    private void DragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _controller.HandleDragAreaMouseLeftButtonDown(sender, e);
    }

    private void DragArea_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _controller.HandleDragAreaMouseMove(e);
    }

    private void DragArea_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _controller.HandleDragAreaMouseLeftButtonUp();
    }

    private void Window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _controller.HandleMouseEnter();
    }

    private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _controller.HandleMouseLeave();
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        _controller.HandleKeyDown(e);
    }

    public void CenterUpOnScreen(Forms.Screen screen)
    {
        _controller.CenterUpOnScreen(screen);
    }
}
