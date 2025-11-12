using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ManagementEmployee.Models;
using ManagementEmployee.Services;

namespace ManagementEmployee.ViewModels
{
    public class ActivityLogViewModel : BaseViewModel
    {
        private readonly ActivityLogService _service;

        private DateTime? _fromDate = DateTime.Today.AddDays(-7);
        private DateTime? _toDate = DateTime.Today;
        private int _selectedUserId;
        private string _keyword = string.Empty;
        private int _pageSize = 200;
        private int _totalItems;

        public event EventHandler<string>? MessageShown;
        public event EventHandler<string>? ErrorShown;
        private void ShowMessage(string m) => MessageShown?.Invoke(this, m);
        private void ShowError(string m) => ErrorShown?.Invoke(this, m);

        public ObservableCollection<Models.ActivityLogDto> Logs { get; } = new();
        public ObservableCollection<UserLookupItem> Users { get; } = new();
        public ObservableCollection<int> PageSizeOptions { get; } = new(new[] { 50, 100, 200, 500 });

        public ICommand RefreshCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        public ActivityLogViewModel(ActivityLogService service)
        {
            _service = service;

            RefreshCommand = new AsyncRelayCommand(async _ => await RefreshAsync());
            ExportCommand = new AsyncRelayCommand(async _ => await ExportAsync());
            ClearFiltersCommand = new AsyncRelayCommand(async _ => await ClearFiltersAsync());
        }

        public DateTime? FromDate { get => _fromDate; set => SetProperty(ref _fromDate, value); }
        public DateTime? ToDate { get => _toDate; set => SetProperty(ref _toDate, value); }
        public int SelectedUserId { get => _selectedUserId; set => SetProperty(ref _selectedUserId, value); }
        public string Keyword { get => _keyword; set => SetProperty(ref _keyword, value); }
        public int PageSize { get => _pageSize; set => SetProperty(ref _pageSize, value); }
        public int TotalItems { get => _totalItems; private set => SetProperty(ref _totalItems, value); }

        public async Task InitializeAsync()
        {
            await LoadUsersAsync();
            await RefreshAsync();
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                var list = await _service.GetActiveUsersAsync();
                Users.Clear();
                Users.Add(new UserLookupItem { UserId = 0, DisplayName = "Tất cả người dùng" });
                foreach (var u in list) Users.Add(u);
            }
            catch (Exception ex)
            {
                ShowError($"Không thể tải danh sách người dùng: {ex.Message}");
            }
        }

        private async Task RefreshAsync()
        {
            try
            {
                IsLoading = true;

                var from = FromDate?.Date;
                DateTime? to = ToDate?.Date.AddDays(1).AddTicks(-1);
                int? userId = SelectedUserId > 0 ? SelectedUserId : (int?)null;
                string? kw = string.IsNullOrWhiteSpace(Keyword) ? null : Keyword;

                var logs = await _service.GetLogsAsync(from, to, userId, kw, pageNumber: 1, pageSize: PageSize);

                Logs.Clear();
                foreach (var l in logs) Logs.Add(l);

                TotalItems = Logs.Count;
                ShowMessage($"Đã tải {TotalItems} bản ghi.");
            }
            catch (Exception ex)
            {
                ShowError($"Không thể tải nhật ký: {ex.Message}");
            }
            finally { IsLoading = false; }
        }

        private async Task ExportAsync()
        {
            try
            {
                IsLoading = true;
                if (Logs.Count == 0) { ShowError("Không có dữ liệu để xuất."); return; }
                var path = await _service.ExportToExcelAsync(Logs);
                ShowMessage("Xuất file thành công!");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ShowError($"Không thể xuất file: {ex.Message}");
            }
            finally { IsLoading = false; }
        }

        private async Task ClearFiltersAsync()
        {
            FromDate = DateTime.Today.AddDays(-7);
            ToDate = DateTime.Today;
            SelectedUserId = 0;
            Keyword = string.Empty;
            PageSize = 200;
            await RefreshAsync();
        }
    }
}
