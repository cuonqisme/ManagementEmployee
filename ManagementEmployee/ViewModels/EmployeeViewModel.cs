using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ManagementEmployee.Models;
using ManagementEmployee.Services;

namespace ManagementEmployee.ViewModels
{
    public class EmployeeViewModel : BaseViewModel
    {
        private readonly int _userId;
        private readonly ActivityLogService _activityLogService = new ActivityLogService();

        // Thông tin hiển thị
        public string EmployeeName { get => _employeeName; private set => SetProperty(ref _employeeName, value); }
        public string Email { get => _email; private set => SetProperty(ref _email, value); }
        public string DepartmentName { get => _departmentName; private set => SetProperty(ref _departmentName, value); }
        public string Position { get => _position; private set => SetProperty(ref _position, value); }
        public string Phone { get => _phone; private set => SetProperty(ref _phone, value); }
        public string Address { get => _address; private set => SetProperty(ref _address, value); }
        public string Gender { get => _gender; private set => SetProperty(ref _gender, value); }
        public string DobDisplay { get => _dobDisplay; private set => SetProperty(ref _dobDisplay, value); }
        public string HireDateDisplay { get => _hireDateDisplay; private set => SetProperty(ref _hireDateDisplay, value); }
        public string ActiveDisplay { get => _activeDisplay; private set => SetProperty(ref _activeDisplay, value); }

        public string TodayStatusText { get => _todayStatusText; private set => SetProperty(ref _todayStatusText, value); }
        public string TodayDateDisplay => DateTime.Now.ToString("dddd, dd/MM/yyyy");
        public string NowDisplay => DateTime.Now.ToString("HH:mm");

        public bool CanCheckIn { get => _canCheckIn; private set { if (SetProperty(ref _canCheckIn, value)) CheckInCommand.RaiseCanExecuteChanged(); } }
        public bool CanCheckOut { get => _canCheckOut; private set { if (SetProperty(ref _canCheckOut, value)) CheckOutCommand.RaiseCanExecuteChanged(); } }

        public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }
        public string UnreadTips { get => _unreadTips; private set => SetProperty(ref _unreadTips, value); }

        // Lịch sử chấm công
        public ObservableCollection<ActivityLogDto> RecentAttendance { get; } = new ObservableCollection<ActivityLogDto>();

        // Commands
        public AsyncRelayCommand RefreshCommand { get; }
        public AsyncRelayCommand CheckInCommand { get; }
        public AsyncRelayCommand CheckOutCommand { get; }

        // Backing fields
        private string _employeeName = "";
        private string _email = "";
        private string _departmentName = "";
        private string _position = "";
        private string _phone = "";
        private string _address = "";
        private string _gender = "";
        private string _dobDisplay = "";
        private string _hireDateDisplay = "";
        private string _activeDisplay = "";
        private string _todayStatusText = "";
        private bool _canCheckIn = true;
        private bool _canCheckOut = false;
        private string _statusMessage = "";
        private string _unreadTips = "";

        public EmployeeViewModel()
        {
            _userId = Math.Max(AppSession.CurrentUserId ?? 0, 0);
            RefreshCommand = new AsyncRelayCommand(_ => InitializeAsync());
            CheckInCommand = new AsyncRelayCommand(_ => CheckInAsync(), _ => CanCheckIn);
            CheckOutCommand = new AsyncRelayCommand(_ => CheckOutAsync(), _ => CanCheckOut);
        }

        public EmployeeViewModel(int userId) : this()
        {
            _userId = userId;
        }

        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                await LoadProfileAsync();
                await LoadAttendanceAsync();
                UpdateTodayState();
                StatusMessage = "Đã tải thông tin.";
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi tải dữ liệu: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadProfileAsync()
        {
            using var db = new ManagementEmployeeContext();

            // Lấy user hiện tại
            var user = db.Users
                         .Where(u => u.UserId == _userId)
                         .Select(u => new
                         {
                             u.Email,
                             Emp = u.Employee
                         })
                         .FirstOrDefault();

            if (user?.Emp == null)
            {
                EmployeeName = "Người dùng";
                Email = user?.Email ?? "";
                DepartmentName = "—";
                Position = "—";
                Phone = "—";
                Address = "—";
                Gender = "—";
                DobDisplay = "—";
                HireDateDisplay = "—";
                ActiveDisplay = "Không xác định";
                return;
            }

            // DepartmentName
            string deptName = "—";
            if (user.Emp.DepartmentId != null)
            {
                var dept = db.Departments.FirstOrDefault(d => d.DepartmentId == user.Emp.DepartmentId);
                if (dept != null) deptName = dept.DepartmentName;
            }

            EmployeeName = user.Emp.FullName ?? "—";
            Email = user.Email ?? "";
            DepartmentName = deptName;
            Position = string.IsNullOrWhiteSpace(user.Emp.Position) ? "—" : user.Emp.Position;
            Phone = string.IsNullOrWhiteSpace(user.Emp.Phone) ? "—" : user.Emp.Phone;
            Address = string.IsNullOrWhiteSpace(user.Emp.Address) ? "—" : user.Emp.Address;
            Gender = string.IsNullOrWhiteSpace(user.Emp.Gender) ? "—" : user.Emp.Gender;
            DobDisplay = user.Emp.DateOfBirth != default ? user.Emp.DateOfBirth.ToString("dd/MM/yyyy") : "—";
            HireDateDisplay = user.Emp.HireDate != default ? user.Emp.HireDate.ToString("dd/MM/yyyy") : "—";
            ActiveDisplay = user.Emp.IsActive ? "Đang làm việc" : "Tạm nghỉ";

            await Task.CompletedTask;
        }

        private async Task LoadAttendanceAsync()
        {
            // Lấy 14 ngày gần nhất của chính user
            var from = DateTime.Today.AddDays(-14);
            var to = DateTime.Today.AddDays(1).AddTicks(-1);

            var logs = await _activityLogService.GetLogsAsync(
                from: from, to: to, userId: _userId, keyword: null, pageNumber: 1, pageSize: 500);

            // Chỉ lấy hành động chấm công
            var items = logs
                .Where(l => string.Equals(l.Action, "CheckIn", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(l.Action, "CheckOut", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(l => l.CreatedAt)
                .ToList();

            RecentAttendance.Clear();
            foreach (var l in items)
                RecentAttendance.Add(l);

            // Gợi ý ngắn
            UnreadTips = $"14 ngày gần đây: {items.Count} bản ghi";
        }

        private void UpdateTodayState()
        {
            var today = DateTime.Today;
            var todayLogs = RecentAttendance
                .Where(l => l.CreatedAt.Date == today)
                .OrderBy(l => l.CreatedAt)
                .ToList();

            bool hasIn = todayLogs.Any(l => string.Equals(l.Action, "CheckIn", StringComparison.OrdinalIgnoreCase));
            bool hasOut = todayLogs.Any(l => string.Equals(l.Action, "CheckOut", StringComparison.OrdinalIgnoreCase));

            CanCheckIn = !hasIn;
            CanCheckOut = hasIn && !hasOut;

            if (!hasIn && !hasOut) TodayStatusText = "Hôm nay chưa check in.";
            else if (hasIn && !hasOut) TodayStatusText = "Đã check in. Bạn có thể check out khi hoàn tất.";
            else TodayStatusText = "Hôm nay đã check out.";
        }

        private async Task CheckInAsync()
        {
            try
            {
                IsLoading = true;
                await _activityLogService.LogAsync("CheckIn", "Attendance", details: $"Check in lúc {DateTime.Now:HH:mm}", userId: _userId);
                StatusMessage = "Đã check in.";
                await LoadAttendanceAsync();
                UpdateTodayState();
                ShowMessage("Check in thành công.");
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi check in: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CheckOutAsync()
        {
            try
            {
                IsLoading = true;
                await _activityLogService.LogAsync("CheckOut", "Attendance", details: $"Check out lúc {DateTime.Now:HH:mm}", userId: _userId);
                StatusMessage = "Đã check out.";
                await LoadAttendanceAsync();
                UpdateTodayState();
                ShowMessage("Check out thành công.");
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi check out: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
