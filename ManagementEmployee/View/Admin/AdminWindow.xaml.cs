using ManagementEmployee.Models;
using ManagementEmployee.Services;
using ManagementEmployee.ViewModels;
using ManagementEmployee.ViewModels.Admin;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ManagementEmployee.View.Admin
{
    public partial class AdminWindow : Window
    {
        private AdminWindowViewModel VM => DataContext as AdminWindowViewModel;
        private readonly int _currentUserId;
        public AdminWindow(int currentUserId = 1, string adminDisplayName = "Administrator")
        {
            InitializeComponent();
            _currentUserId = currentUserId;
            txtAdminName.Text = string.IsNullOrWhiteSpace(adminDisplayName) ? "Administrator" : adminDisplayName;

            Loaded += AdminWindow_Loaded;
        }
        private async void AdminWindow_Loaded(object sender, RoutedEventArgs e)
        {

            NavigateReportHome();
            //await LoadNotificationCountAsync();
        }

        private void NavigateReportHome()
        {
            SetActiveSidebarButton(btnHome);
            DashboardGrid.Visibility = Visibility.Collapsed;
            ContentFrame.Visibility = Visibility.Visible;
            ContentFrame.Content = new ReportPage();
        }

        private void HomeButton(object sender, RoutedEventArgs e) => NavigateReportHome();

        private void AccountManagerButton(object sender, RoutedEventArgs e)
        {
            SetActiveSidebarButton(btnAccount);
            DashboardGrid.Visibility = Visibility.Collapsed;
            ContentFrame.Visibility = Visibility.Visible;
            NavigateOrPlaceholder("View/Admin/AccountManagerPage.xaml", "Account Manager");
        }
        private void DepartmentButton(object sender, RoutedEventArgs e)
        {
            SetActiveSidebarButton(btnDept);
            DashboardGrid.Visibility = Visibility.Collapsed;
            ContentFrame.Visibility = Visibility.Visible;
            ContentFrame.Content = new DepartmentManagerPage();
        }

        private void PayrollButton(object sender, RoutedEventArgs e)
        {
            SetActiveSidebarButton(btnPayroll);
            DashboardGrid.Visibility = Visibility.Collapsed;
            ContentFrame.Visibility = Visibility.Visible;
            ContentFrame.Content = new PayrollManagerPage();
        }
        private void AttendanceButton(object sender, RoutedEventArgs e)
        {
            SetActiveSidebarButton(btnAttendance);
            DashboardGrid.Visibility = Visibility.Collapsed;
            ContentFrame.Visibility = Visibility.Visible;
            ContentFrame.Content = new AttendanceManagerPage();
        }
        private void ActivityLogButton(object sender, RoutedEventArgs e)
        {
            SetActiveSidebarButton(btnActivityLog);
            DashboardGrid.Visibility = Visibility.Collapsed;
            ContentFrame.Visibility = Visibility.Visible;
            ContentFrame.Content = new ActivityLogPage();
        }

        private void BackupButton(object sender, RoutedEventArgs e)
        {
            ContentFrame.Visibility = Visibility.Visible;
            DashboardGrid.Visibility = Visibility.Collapsed;

            var ctx = new ManagementEmployeeContext();


            var activityLogService = new ActivityLogService();

          
            var backupService = new BackupService(ctx, activityLogService);

            var vm = new BackupViewModel(backupService);
            ContentFrame.Content = new BackupPage(vm);
        }


        private void NotificationButton(object sender, RoutedEventArgs e)
        {
            SetActiveSidebarButton(btnNotify);
            DashboardGrid.Visibility = Visibility.Collapsed;
            ContentFrame.Visibility = Visibility.Visible;
            ContentFrame.Navigate(new NotificationPage());
        }

        private void Button_Logout(object sender, RoutedEventArgs e)
        {
            try
            {
         
                (this.DataContext as IDisposable)?.Dispose();

                // Dọn phiên đăng nhập
                AppSession.SignOut();

    
                var login = new LoginWindow();
                login.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi đăng xuất: " + ex.Message, "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

  
        private void SetActiveSidebarButton(Button active)
        {
            foreach (var child in buttonList.Children)
            {
                if (child is Button b)
                {
                    b.Foreground = Brushes.White;
                    b.Background = Brushes.Transparent;
                }
            }
            active.Background = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF)); // #40FFFFFF
        }

        private void ShowDashboard(bool show)
        {
            DashboardGrid.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            ContentFrame.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        }

        private void NavigateOrPlaceholder(string relativeUri, string title)
        {
            try
            {
                ShowDashboard(false);
                ContentFrame.Navigate(new Uri(relativeUri, UriKind.Relative));
            }
            catch
            {
                var placeholder = new Border
                {
                    CornerRadius = new CornerRadius(16),
                    Background = new SolidColorBrush(Color.FromArgb(0x10, 0xFF, 0xFF, 0xFF)),
                    Padding = new Thickness(24),
                    Child = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock
                            {
                                Text = title, Foreground = Brushes.White,
                                FontSize = 20, FontWeight = FontWeights.SemiBold,
                                Margin = new Thickness(0,0,0,8)
                            },
                            new TextBlock
                            {
                                Text = "This page has not been created yet. Replace with your real Page.",
                                Foreground = new SolidColorBrush(Color.FromRgb(214,215,242)),
                                FontSize = 13
                            }
                        }
                    }
                };
                ContentFrame.Content = placeholder;
            }
        }

        // ===== Nhận yêu cầu điều hướng từ VM =====
        private void VM_RequestSection(AdminSection section)
        {
            switch (section)
            {
                case AdminSection.Dashboard:
                    ShowDashboard(true);
                    break;

                case AdminSection.AccountManager:
                    NavigateOrPlaceholder("View/Admin/AccountManagerPage.xaml", "Account Manager");
                    break;

                case AdminSection.Department:
                    ShowDashboard(false);
                    ContentFrame.Content = new DepartmentManagerPage();
                    break;

                case AdminSection.Payroll:
                    ShowDashboard(false);
                    ContentFrame.Content = new PayrollManagerPage();
                    break;

                case AdminSection.Attendance:
                    ShowDashboard(false);
                    ContentFrame.Content = new AttendanceManagerPage();
                    break;

                case AdminSection.Notifications:
                    NavigateOrPlaceholder("View/Notifications/NotificationsPage.xaml", "Notifications");
                    break;
            }
        }

        private void VM_RequestLogout()
        {
            var login = new ManagementEmployee.LoginWindow();
            Application.Current.MainWindow = login;
            login.Show();
            Close();
        }
    }
}
