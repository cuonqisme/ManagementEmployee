using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using ManagementEmployee.Services;

namespace ManagementEmployee.ViewModels;

public class BackupViewModel : BaseViewModel
{
    private readonly BackupService _backupService;

    private string _backupDirectory;
    private string _restoreFilePath = string.Empty;
    private bool _overwriteExisting;
    private string _statusMessage = string.Empty;
    private DateTime? _lastBackupAt;
    private string? _lastBackupFile;

    public BackupViewModel(BackupService backupService)
    {
        _backupService = backupService;

        _backupDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        HistoryEntries = new ObservableCollection<BackupHistoryItem>();

        BackupCommand = new AsyncRelayCommand(BackupAsync);
        RestoreCommand = new AsyncRelayCommand(RestoreAsync);
    }

    public string BackupDirectory
    {
        get => _backupDirectory;
        set => SetProperty(ref _backupDirectory, value);
    }

    public string RestoreFilePath
    {
        get => _restoreFilePath;
        set => SetProperty(ref _restoreFilePath, value);
    }

    public bool OverwriteExisting
    {
        get => _overwriteExisting;
        set => SetProperty(ref _overwriteExisting, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public DateTime? LastBackupAt
    {
        get => _lastBackupAt;
        private set => SetProperty(ref _lastBackupAt, value);
    }

    public string? LastBackupFile
    {
        get => _lastBackupFile;
        private set => SetProperty(ref _lastBackupFile, value);
    }

    public ObservableCollection<BackupHistoryItem> HistoryEntries { get; }

    public AsyncRelayCommand BackupCommand { get; }
    public AsyncRelayCommand RestoreCommand { get; }

    private async Task BackupAsync()
    {
        try
        {
            IsLoading = true;

            if (string.IsNullOrWhiteSpace(BackupDirectory))
            {
                throw new InvalidOperationException("Vui lòng chọn thư mục lưu trữ.");
            }

            var result = await _backupService.BackupEmployeesAsync(BackupDirectory);

            LastBackupAt = DateTime.Now;
            LastBackupFile = result.FilePath;
            StatusMessage = $"Đã sao lưu {result.EmployeeCount} nhân viên vào \"{result.FilePath}\".";
            HistoryEntries.Insert(0, new BackupHistoryItem("Sao lưu", StatusMessage, DateTime.Now));

            ShowMessage("Sao lưu thành công!");
        }
        catch (Exception ex)
        {
            ShowError($"Không thể sao lưu: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RestoreAsync()
    {
        try
        {
            IsLoading = true;

            if (string.IsNullOrWhiteSpace(RestoreFilePath) || !File.Exists(RestoreFilePath))
            {
                throw new FileNotFoundException("Vui lòng chọn tệp sao lưu hợp lệ.", RestoreFilePath);
            }

            var result = await _backupService.RestoreEmployeesAsync(RestoreFilePath, OverwriteExisting);
            StatusMessage = $"Phục hồi thành công. Tạo mới: {result.Created}, cập nhật: {result.Updated}, bỏ qua: {result.Skipped}.";
            HistoryEntries.Insert(0, new BackupHistoryItem("Phục hồi", StatusMessage, DateTime.Now));

            ShowMessage(StatusMessage);
        }
        catch (Exception ex)
        {
            ShowError($"Không thể phục hồi: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}

public record BackupHistoryItem(string Action, string Detail, DateTime Timestamp);

