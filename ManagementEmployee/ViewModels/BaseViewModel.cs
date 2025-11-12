using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ManagementEmployee.ViewModels;

public abstract class BaseViewModel : INotifyPropertyChanged
{
    private bool _isLoading;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<string>? MessageShown;
    public event EventHandler<string>? ErrorShown;

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected void ShowMessage(string message)
        => MessageShown?.Invoke(this, message);

    protected void ShowError(string error)
        => ErrorShown?.Invoke(this, error);
}

