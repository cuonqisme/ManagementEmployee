using System.Windows;
using System.Windows.Controls;
using ManagementEmployee.Models;
using ManagementEmployee.Services;
using ManagementEmployee.ViewModels;

namespace ManagementEmployee.View.Admin
{
    public partial class NotificationPage : Page
    {
        private NotificationViewModel? _viewModel;
        private ManagementEmployeeContext? _dbForService;
        private ManagementEmployeeContext? _dbForViewModel;

        public NotificationPage()
        {
            InitializeComponent();

            if (!AppSession.IsAuthenticated)
            {
                MessageBox.Show("Phiên đăng nhập đã hết. Vui lòng đăng nhập lại.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Tạo context tách biệt
            _dbForService = new ManagementEmployeeContext();
            _dbForViewModel = new ManagementEmployeeContext();

            var service = new NotificationService(_dbForService);
            var currentUserId = AppSession.CurrentUserId!.Value;

            _viewModel = new NotificationViewModel(service, _dbForViewModel, currentUserId);
            DataContext = _viewModel;

            _viewModel.MessageShown += OnMessageShown;
            _viewModel.ErrorShown += OnErrorShown;

            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            await _viewModel.InitializeAsync();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.MessageShown -= OnMessageShown;
                _viewModel.ErrorShown -= OnErrorShown;
                _viewModel.Dispose();
                _viewModel = null;
            }

            _dbForService?.Dispose(); _dbForService = null;
            _dbForViewModel?.Dispose(); _dbForViewModel = null;
        }

        private void OnMessageShown(object? sender, string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                Dispatcher.Invoke(() => MessageBox.Show(message, "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information));
        }

        private void OnErrorShown(object? sender, string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                Dispatcher.Invoke(() => MessageBox.Show(message, "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error));
        }
    }
}
