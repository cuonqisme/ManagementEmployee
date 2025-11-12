using System;
using System.Threading.Tasks;
using ManagementEmployee.Models;
using Microsoft.EntityFrameworkCore;

namespace ManagementEmployee.ViewModels.Admin
{
    public enum AdminSection { Dashboard, Profile, AccountManager, Department, Payroll, Attendance, Notifications }

    public sealed class AdminWindowViewModel : ViewModelBase
    {
        private readonly int _currentUserId;
        private string _adminDisplayName;
        private string _employeesCount = "—";
        private string _departmentsCount = "—";
        private string _leavePendingCount = "—";
        private string _notificationCount = "0";

        public AdminWindowViewModel(int currentUserId, string adminDisplayName)
        {
            _currentUserId = currentUserId;
            AdminDisplayName = string.IsNullOrWhiteSpace(adminDisplayName) ? "Administrator" : adminDisplayName;

            LoadedCommand = new AsyncRelayCommand(async _ => await OnLoadedAsync());
            HomeCommand = new RelayCommand(_ => RequestSection?.Invoke(AdminSection.Dashboard));
            ProfileCommand = new RelayCommand(_ => RequestSection?.Invoke(AdminSection.Profile));
            AccountManagerCommand = new RelayCommand(_ => RequestSection?.Invoke(AdminSection.AccountManager));
            DepartmentCommand = new RelayCommand(_ => RequestSection?.Invoke(AdminSection.Department));
            PayrollCommand = new RelayCommand(_ => RequestSection?.Invoke(AdminSection.Payroll));
            AttendanceCommand = new RelayCommand(_ => RequestSection?.Invoke(AdminSection.Attendance));
            NotificationsCommand = new RelayCommand(_ => RequestSection?.Invoke(AdminSection.Notifications));
            LogoutCommand = new RelayCommand(_ => RequestLogout?.Invoke());
        }

        // ===== Bindable props (code-behind sẽ bơm vào TextBlock hiện tại) =====
        public string AdminDisplayName
        {
            get => _adminDisplayName;
            set => SetProperty(ref _adminDisplayName, value);
        }
        public string EmployeesCount
        {
            get => _employeesCount;
            private set => SetProperty(ref _employeesCount, value);
        }
        public string DepartmentsCount
        {
            get => _departmentsCount;
            private set => SetProperty(ref _departmentsCount, value);
        }
        public string LeavePendingCount
        {
            get => _leavePendingCount;
            private set => SetProperty(ref _leavePendingCount, value);
        }
        public string NotificationCount
        {
            get => _notificationCount;
            private set => SetProperty(ref _notificationCount, value);
        }

        // ===== Commands =====
        public AsyncRelayCommand LoadedCommand { get; }
        public RelayCommand HomeCommand { get; }
        public RelayCommand ProfileCommand { get; }
        public RelayCommand AccountManagerCommand { get; }
        public RelayCommand DepartmentCommand { get; }
        public RelayCommand PayrollCommand { get; }
        public RelayCommand AttendanceCommand { get; }
        public RelayCommand NotificationsCommand { get; }
        public RelayCommand LogoutCommand { get; }

        // ===== Events để View xử lý điều hướng/hiển thị =====
        public event Action<AdminSection> RequestSection;
        public event Action RequestLogout;

        // ===== Lifecycle =====
        private async Task OnLoadedAsync()
        {
            await RefreshDashboardAsync();
            await RefreshNotificationAsync();
        }

        private async Task RefreshDashboardAsync()
        {
            try
            {
                using var db = new ManagementEmployeeContext();
                var empCountTask = db.Employees.CountAsync(e => e.IsActive);
                var deptCountTask = db.Departments.CountAsync();
                var leavePendTask = db.LeaveRequests.CountAsync(lr => lr.Status == 0);
                await Task.WhenAll(empCountTask, deptCountTask, leavePendTask);

                EmployeesCount = empCountTask.Result.ToString();
                DepartmentsCount = deptCountTask.Result.ToString();
                LeavePendingCount = leavePendTask.Result.ToString();
            }
            catch
            {
                EmployeesCount = "—";
                DepartmentsCount = "—";
                LeavePendingCount = "—";
            }
        }

        private async Task RefreshNotificationAsync()
        {
            try
            {
                using var db = new ManagementEmployeeContext();
                var count = await db.Notifications
                    .CountAsync(n => (n.ReceiverUserId == _currentUserId || n.ReceiverUserId == null) && !n.IsRead);
                NotificationCount = count.ToString();
            }
            catch
            {
                NotificationCount = "0";
            }
        }
    }
}
