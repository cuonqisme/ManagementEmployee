using ManagementEmployee.Services;
using ManagementEmployee.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ManagementEmployee.View.Admin
{
    /// <summary>
    /// Interaction logic for ReportPage.xaml
    /// </summary>
    public partial class ReportPage : Page
    {
        public ReportPage()
        {
            InitializeComponent();

            // Tạo ViewModel (có thể thay bằng DI nếu bạn có container)
            var vm = new ReportViewModel(new StatisticService(), new ReportService());
            DataContext = vm;

            vm.MessageShown += OnMessageShown;
            vm.ErrorShown += OnErrorShown;

            Loaded += async (_, __) => await vm.LoadStatisticsAsync();
            Unloaded += (_, __) =>
            {
                vm.MessageShown -= OnMessageShown;
                vm.ErrorShown -= OnErrorShown;
            };
        }

        private void OnMessageShown(object? sender, string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                MessageBox.Show(message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnErrorShown(object? sender, string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                MessageBox.Show(message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
