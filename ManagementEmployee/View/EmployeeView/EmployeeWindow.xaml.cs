using System;
using System.Windows;
using ManagementEmployee.ViewModels;

namespace ManagementEmployee.View.EmployeeView
{
    public partial class EmployeeWindow : Window
    {
        private readonly EmployeeViewModel _vm;

        // Dùng AppSession.CurrentUserId (nếu có)
        public EmployeeWindow()
        {
            InitializeComponent();
            _vm = new EmployeeViewModel();
            DataContext = _vm;
        }

        // Overload nếu bạn muốn truyền userId thủ công
        public EmployeeWindow(int userId)
        {
            InitializeComponent();
            _vm = new EmployeeViewModel(userId);
            DataContext = _vm;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await _vm.InitializeAsync();
        }
    }
}
