using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TimeWidget.ViewModels;

/// <summary>
/// Base class for observable view models.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Updates a backing field and raises <see cref="PropertyChanged"/> when the value changes.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
    /// <param name="field">The backing field to update.</param>
    /// <param name="value">The new value.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns><see langword="true"/> when the value changed; otherwise, <see langword="false"/>.</returns>
    protected bool SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

