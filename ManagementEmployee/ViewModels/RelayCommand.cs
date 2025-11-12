using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ManagementEmployee.ViewModels
{
    /// <summary>
    /// RelayCommand đồng bộ (Action).
    /// Hỗ trợ cả ctor không tham số và có tham số object?.
    /// </summary>
    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            if (execute == null) throw new ArgumentNullException(nameof(execute));
            _execute = _ => execute();
            _canExecute = canExecute is null ? null : (_ => canExecute());
        }

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>
    /// AsyncRelayCommand không tham số (Func<Task>).
    /// Dùng cho các lệnh async không cần parameter.
    /// </summary>
    public sealed class AsyncRelayCommand : ICommand
    {
        private readonly Func<object?, Task> _executeAsync;
        private readonly Func<object?, bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
        {
            if (executeAsync == null) throw new ArgumentNullException(nameof(executeAsync));
            _executeAsync = _ => executeAsync();
            _canExecute = canExecute is null ? null : (_ => canExecute());
        }

        public AsyncRelayCommand(Func<object?, Task> executeAsync, Func<object?, bool>? canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public bool IsExecuting => _isExecuting;

        public bool CanExecute(object? parameter)
            => !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;
            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();
                await _executeAsync(parameter);
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// AsyncRelayCommand generic có tham số kiểu T (ví dụ: AsyncRelayCommand<int?>).
    /// Phù hợp với code như: new AsyncRelayCommand<int?>(MarkAsReadAsync)
    /// </summary>
    public sealed class AsyncRelayCommand<T> : ICommand
    {
        private readonly Func<T?, Task> _executeAsync;
        private readonly Func<T?, bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<T?, Task> executeAsync, Func<T?, bool>? canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public bool IsExecuting => _isExecuting;

        public bool CanExecute(object? parameter)
        {
            if (_isExecuting) return false;
            if (_canExecute == null) return true;
            return _canExecute(Cast(parameter));
        }

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;
            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();
                await _executeAsync(Cast(parameter));
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        private static T? Cast(object? parameter)
        {
            if (parameter is null) return default;
            // Nếu T là nullable hoặc reference type, phép cast trực tiếp sẽ ổn.
            // Trường hợp binding CommandParameter là string/boxed -> cố gắng convert.
            if (parameter is T t) return t;

            try
            {
                // Hỗ trợ convert từ string sang số/enum… nếu cần
                return (T?)Convert.ChangeType(parameter, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
