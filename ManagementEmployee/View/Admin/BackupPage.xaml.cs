using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ManagementEmployee.ViewModels;
using Microsoft.Win32;

namespace ManagementEmployee.View.Admin
{
    public partial class BackupPage : Page
    {
        private BackupViewModel? _vm;

        public BackupPage(BackupViewModel viewModel)
        {
            InitializeComponent();
            _vm = viewModel;
            DataContext = viewModel;

            viewModel.MessageShown += OnMessageShown;
            viewModel.ErrorShown += OnErrorShown;

            Loaded += BackupPage_Loaded;
            Unloaded += BackupPage_Unloaded;
        }

        private void BackupPage_Loaded(object sender, RoutedEventArgs e)
        {
            // nothing special to load; ViewModel mặc định đã sẵn sàng
        }

        private void BackupPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_vm != null)
            {
                _vm.MessageShown -= OnMessageShown;
                _vm.ErrorShown -= OnErrorShown;
            }
        }

    
        // Người dùng chọn 1 file "gợi ý"
        private void BrowseBackupDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (_vm == null) return;

            var sfd = new SaveFileDialog
            {
                Title = "Chọn thư mục đích (chọn tên file bất kỳ để lấy thư mục)",
                Filter = "JSON (*.json)|*.json|All files (*.*)|*.*",
                FileName = $"employees_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                OverwritePrompt = false,
                AddExtension = true
            };

            if (!string.IsNullOrWhiteSpace(_vm.BackupDirectory) && Directory.Exists(_vm.BackupDirectory))
            {
                sfd.InitialDirectory = _vm.BackupDirectory;
            }

            var ok = sfd.ShowDialog();
            if (ok == true)
            {
                var dir = Path.GetDirectoryName(sfd.FileName);
                if (!string.IsNullOrEmpty(dir))
                {
                    _vm.BackupDirectory = dir;
                }
            }
        }

        private void BrowseRestoreFile_Click(object sender, RoutedEventArgs e)
        {
            if (_vm == null) return;

            var ofd = new OpenFileDialog
            {
                Title = "Chọn tệp sao lưu",
                Filter = "Backup JSON (*.json)|*.json|All files (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (!string.IsNullOrWhiteSpace(_vm.RestoreFilePath))
            {
                try
                {
                    var dir = Path.GetDirectoryName(_vm.RestoreFilePath);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    {
                        ofd.InitialDirectory = dir;
                        ofd.FileName = Path.GetFileName(_vm.RestoreFilePath);
                    }
                }
                catch { /* ignore */ }
            }

            var ok = ofd.ShowDialog();
            if (ok == true)
            {
                _vm.RestoreFilePath = ofd.FileName;
            }
        }

        private void OnMessageShown(object? sender, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            Dispatcher.Invoke(() =>
                MessageBox.Show(message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information));
        }

        private void OnErrorShown(object? sender, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            Dispatcher.Invoke(() =>
                MessageBox.Show(message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error));
        }
    }
}
