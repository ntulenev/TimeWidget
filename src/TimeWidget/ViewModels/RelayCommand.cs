using System.Windows.Input;

namespace TimeWidget.ViewModels;

/// <summary>
/// Simple <see cref="ICommand"/> implementation backed by delegates.
/// </summary>
public sealed class RelayCommand : ICommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand"/> class.
    /// </summary>
    /// <param name="execute">The action to execute.</param>
    /// <param name="canExecute">The optional predicate that determines whether the command can execute.</param>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(execute);

        _execute = execute;
        _canExecute = canExecute;
    }

    /// <summary>
    /// Occurs when the command's ability to execute changes.
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// Determines whether the command can execute.
    /// </summary>
    /// <param name="parameter">The command parameter.</param>
    /// <returns><see langword="true"/> when the command can execute; otherwise, <see langword="false"/>.</returns>
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="parameter">The command parameter.</param>
    public void Execute(object? parameter) => _execute();

    /// <summary>
    /// Raises <see cref="CanExecuteChanged"/>.
    /// </summary>
    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;
}

