using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ManagementEmployee.Models;

namespace ManagementEmployee.View.Admin
{
    public partial class AdminWindow : Window
    {
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
            SetActiveSidebarButton(btnHome);
            ShowDashboard(true);

            await LoadDashboardMetricsAsync();
            await LoadNotificationCountAsync();
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

        private void HomeButton(object sender, RoutedEventArgs e)
        {
            SetActiveSidebarButton(btnHome);
            ShowDashboard(true);
        }

        private void ProfileButton(object sender, RoutedEventArgs e)
        {
            SetActiveSidebarButton(btnProfile);
            NavigateOrPlaceholder("View/Profile/ProfilePage.xaml", "Your Profile");
        }

        private void AccountManagerButton(object sender, RoutedEventArgs e)
        {
            SetActiveSidebarButton(btnAccount);
            NavigateOrPlaceholder("View/Admin/AccountManagerPage.xaml", "Account Manager");
        }

        private void CertificateButton(object sender, RoutedEventArgs e)
        {
            SetActiveSidebarButton(btnCertificate);
            NavigateOrPlaceholder("View/Admin/Certificates/CertificatePage.xaml", "Certificates");
        }

        private void NotificationButton(object sender, RoutedEventArgs e)
        {
            SetActiveSidebarButton(btnNotify);
            NavigateOrPlaceholder("View/Notifications/NotificationsPage.xaml", "Notifications");
        }

        private void Button_Logout(object sender, RoutedEventArgs e)
        {
            var login = new ManagementEmployee.LoginWindow();
            Application.Current.MainWindow = login;
            login.Show();
            Close();
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
                                Text = title,
                                Foreground = Brushes.White,
                                FontSize = 20,
                                FontWeight = FontWeights.SemiBold,
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

        private async Task LoadDashboardMetricsAsync()
        {
            try
            {
                using var db = new ManagementEmployeeContext();

                var employees = db.Employees.Where(e => e.IsActive);
                var departments = db.Departments.AsQueryable();
                var leavePending = db.LeaveRequests.Where(lr => lr.Status == 0); // 0=Pending

                var empCountTask = employees.CountAsync();
                var deptCountTask = departments.CountAsync();
                var leavePendingTask = leavePending.CountAsync();

                await Task.WhenAll(empCountTask, deptCountTask, leavePendingTask);

                txtMetricEmployees.Text = empCountTask.Result.ToString();
                txtMetricDepartments.Text = deptCountTask.Result.ToString();
                txtMetricLeavePending.Text = leavePendingTask.Result.ToString();
            }
            catch
            {
                txtMetricEmployees.Text = "—";
                txtMetricDepartments.Text = "—";
                txtMetricLeavePending.Text = "—";
            }
        }

        // ========= Notification Badge =========
        private async Task LoadNotificationCountAsync()
        {
            try
            {
                using var db = new ManagementEmployeeContext();

                int count = await db.Notifications
                    .Where(n => (n.ReceiverUserId == _currentUserId || n.ReceiverUserId == null) && !n.IsRead)
                    .CountAsync();

                txtCountMessageNotRead.Text = count.ToString();
            }
            catch
            {
                txtCountMessageNotRead.Text = "0";
            }
        }
    }
}
