using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ManagementEmployee.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
            return true;
        }
        protected void Raise([CallerMemberName] string propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
