using System.Windows.Controls;
using System.Windows;
using ManagementEmployee.ViewModels;
using ManagementEmployee.Services;

namespace ManagementEmployee.View.Admin
{
    public partial class ActivityLogPage : Page
    {
        public ActivityLogPage()
        {
            InitializeComponent();

            var vm = new ActivityLogViewModel(new ActivityLogService());
            DataContext = vm;

            vm.MessageShown += OnMessage;
            vm.ErrorShown += OnError;

            Loaded += async (_, __) => await vm.InitializeAsync();
            Unloaded += (_, __) =>
            {
                vm.MessageShown -= OnMessage;
                vm.ErrorShown -= OnError;
            };
        }

        private void OnMessage(object? s, string m)
        {
            if (!string.IsNullOrWhiteSpace(m))
                MessageBox.Show(m, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void OnError(object? s, string m)
        {
            if (!string.IsNullOrWhiteSpace(m))
                MessageBox.Show(m, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
