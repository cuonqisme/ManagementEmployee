using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ManagementEmployee.Models;
using ManagementEmployee.Services;
using Microsoft.EntityFrameworkCore;

namespace ManagementEmployee.ViewModels
{
    public class NotificationViewModel : BaseViewModel, IDisposable
    {
        private readonly NotificationService _notificationService;
        private readonly ManagementEmployeeContext _dbContext;

        private int _currentUserId;
        private string _newTitle = string.Empty;
        private string _newContent = string.Empty;
        private int _selectedRecipientType = 0; // 0: All, 1: Department, 2: User
        private int _selectedDepartmentId;
        private int _selectedUserId;
        private int _unreadCount;

        public AsyncRelayCommand SendNotificationCommand { get; }
        public AsyncRelayCommand RefreshNotificationsCommand { get; }
        public AsyncRelayCommand<int?> MarkAsReadCommand { get; }
        public AsyncRelayCommand MarkAllAsReadCommand { get; }
        public AsyncRelayCommand<int?> DeleteNotificationCommand { get; }
        public AsyncRelayCommand ClearFormCommand { get; }

        public ObservableCollection<NotificationDto> Notifications { get; }
        public ObservableCollection<DepartmentDto> Departments { get; }
        public ObservableCollection<UserDto> Users { get; }

        public string CurrentUserDisplayName => AppSession.CurrentUserName ?? AppSession.CurrentUserEmail ?? "Quản trị viên";
        public int CurrentUserId => _currentUserId;

        public NotificationViewModel(NotificationService notificationService, ManagementEmployeeContext dbContext, int userId)
        {
            _notificationService = notificationService;
            _dbContext = dbContext;
            _currentUserId = userId;

            Notifications = new ObservableCollection<NotificationDto>();
            Departments = new ObservableCollection<DepartmentDto>();
            Users = new ObservableCollection<UserDto>();

            SendNotificationCommand = new AsyncRelayCommand(SendNotificationAsync);
            RefreshNotificationsCommand = new AsyncRelayCommand(RefreshNotificationsAsync);
            MarkAsReadCommand = new AsyncRelayCommand<int?>(MarkAsReadAsync);
            MarkAllAsReadCommand = new AsyncRelayCommand(MarkAllAsReadAsync);
            DeleteNotificationCommand = new AsyncRelayCommand<int?>(DeleteNotificationAsync);
            ClearFormCommand = new AsyncRelayCommand(ClearFormAsync);

            _notificationService.NotificationReceived += OnNotificationReceived;
        }

        // --- Properties ---
        public string NewTitle
        {
            get => _newTitle;
            set => SetProperty(ref _newTitle, value);
        }

        public string NewContent
        {
            get => _newContent;
            set => SetProperty(ref _newContent, value);
        }

        public int SelectedRecipientType
        {
            get => _selectedRecipientType;
            set
            {
                if (SetProperty(ref _selectedRecipientType, value))
                {
                    if (value != 1) SelectedDepartmentId = 0;
                    if (value != 2) SelectedUserId = 0;
                    OnPropertyChanged(nameof(IsRecipientAll));
                    OnPropertyChanged(nameof(IsRecipientDepartment));
                    OnPropertyChanged(nameof(IsRecipientUser));
                }
            }
        }

        public int SelectedDepartmentId
        {
            get => _selectedDepartmentId;
            set => SetProperty(ref _selectedDepartmentId, value);
        }

        public int SelectedUserId
        {
            get => _selectedUserId;
            set => SetProperty(ref _selectedUserId, value);
        }

        public bool IsRecipientAll
        {
            get => _selectedRecipientType == 0;
            set { if (value) SelectedRecipientType = 0; }
        }

        public bool IsRecipientDepartment
        {
            get => _selectedRecipientType == 1;
            set { if (value) SelectedRecipientType = 1; }
        }

        public bool IsRecipientUser
        {
            get => _selectedRecipientType == 2;
            set { if (value) SelectedRecipientType = 2; }
        }

        public int UnreadCount
        {
            get => _unreadCount;
            set => SetProperty(ref _unreadCount, value);
        }

        // --- Lifecycle ---
        public async Task InitializeAsync()
        {
            await LoadDepartmentsAndUsersAsync();
            await RefreshNotificationsAsync();
        }

        public async Task LoadDepartmentsAndUsersAsync()
        {
            try
            {
                var departments = await _dbContext.Departments.AsNoTracking().ToListAsync();
                Departments.Clear();
                foreach (var dept in departments)
                {
                    Departments.Add(new DepartmentDto
                    {
                        DepartmentId = dept.DepartmentId,
                        DepartmentName = dept.DepartmentName
                    });
                }

                var users = await _dbContext.Users
                    .AsNoTracking()
                    .Include(u => u.Employee)
                    .Where(u => u.IsActive)
                    .ToListAsync();

                Users.Clear();
                foreach (var user in users)
                {
                    Users.Add(new UserDto
                    {
                        UserId = user.UserId,
                        FullName = user.Employee?.FullName ?? user.Email,
                        Email = user.Email
                    });
                }
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi tải dữ liệu: {ex.Message}");
            }
        }

        public async Task RefreshNotificationsAsync()
        {
            try
            {
                IsLoading = true;

                // includeSent = true để thấy cả thông báo do mình gửi
                var notifications = await _notificationService.GetNotificationsAsync(_currentUserId,
                                                                                    pageSize: 50,
                                                                                    pageNumber: 1,
                                                                                    includeSent: true);
                Notifications.Clear();
                foreach (var n in notifications)
                    Notifications.Add(n);

                UnreadCount = await _notificationService.GetUnreadCountAsync(_currentUserId);

                OnPropertyChanged(nameof(CurrentUserDisplayName));
                OnPropertyChanged(nameof(IsRecipientAll));
                OnPropertyChanged(nameof(IsRecipientDepartment));
                OnPropertyChanged(nameof(IsRecipientUser));
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // --- Actions ---
        private async Task SendNotificationAsync()
        {
            var title = (NewTitle ?? string.Empty).Trim();
            var content = NewContent?.Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                ShowError("Vui lòng nhập tiêu đề");
                return;
            }

            try
            {
                IsLoading = true;
                switch (SelectedRecipientType)
                {
                    case 0:
                        await _notificationService.SendNotificationToAllAsync(_currentUserId, title, content);
                        break;
                    case 1:
                        if (SelectedDepartmentId <= 0) { ShowError("Vui lòng chọn phòng ban"); return; }
                        await _notificationService.SendNotificationToDepartmentAsync(_currentUserId, SelectedDepartmentId, title, content);
                        break;
                    case 2:
                        if (SelectedUserId <= 0) { ShowError("Vui lòng chọn nhân viên"); return; }
                        await _notificationService.SendNotificationToUserAsync(_currentUserId, SelectedUserId, title, content);
                        break;
                }

                ShowMessage("Gửi thông báo thành công!");
                NewTitle = string.Empty;
                NewContent = string.Empty;
                SelectedRecipientType = 0;

                await RefreshNotificationsAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Gửi thông báo thất bại: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task MarkAsReadAsync(int? notificationId)
        {
            try
            {
                if (notificationId == null) return;
                if (await _notificationService.MarkAsReadAsync(notificationId.Value))
                {
                    await RefreshNotificationsAsync();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi: {ex.Message}");
            }
        }

        private async Task MarkAllAsReadAsync()
        {
            try
            {
                IsLoading = true;
                if (await _notificationService.MarkAllAsReadAsync(_currentUserId))
                {
                    ShowMessage("Đã đánh dấu tất cả thông báo đã đọc");
                    await RefreshNotificationsAsync();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteNotificationAsync(int? notificationId)
        {
            try
            {
                if (notificationId == null) return;
                if (await _notificationService.DeleteNotificationAsync(notificationId.Value))
                {
                    await RefreshNotificationsAsync();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi: {ex.Message}");
            }
        }

        private Task ClearFormAsync()
        {
            NewTitle = string.Empty;
            NewContent = string.Empty;
            SelectedRecipientType = 0;
            SelectedDepartmentId = 0;
            SelectedUserId = 0;
            return Task.CompletedTask;
        }

        private void OnNotificationReceived(object? sender, NotificationEventArgs e)
        {
            ShowMessage($"Thông báo mới: {e.Title}");
            _ = RefreshNotificationsAsync();
        }

        public void Dispose()
        {
            _notificationService.NotificationReceived -= OnNotificationReceived;
            _dbContext.Dispose();
        }
    }

    // DTOs cho combobox
    public class DepartmentDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
    }

    public class UserDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
