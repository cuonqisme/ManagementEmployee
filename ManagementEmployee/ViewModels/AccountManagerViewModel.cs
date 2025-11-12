using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using ManagementEmployee.Models;

namespace ManagementEmployee.ViewModels
{
    public sealed class AccountManagerViewModel : INotifyPropertyChanged
    {
        // ======= DTOs / Option items =======
        public sealed class OptionItem { public string Text { get; set; } = ""; public string? Value { get; set; } }
        public sealed class EmployeeRow
        {
            public int EmployeeId { get; set; }
            public string FullName { get; set; }
            public string DepartmentName { get; set; }
            public string Position { get; set; }
            public string GenderText { get; set; }
            public string Phone { get; set; }
            public decimal BaseSalary { get; set; }
            public DateTime HireDate { get; set; }
        }

        // ======= Observable =======
        public ObservableCollection<EmployeeRow> GridItems { get; } = new();
        public ObservableCollection<Department> Departments { get; } = new();
        public ObservableCollection<OptionItem> GenderOptions { get; } = new()
        {
            new OptionItem{ Text="Male",   Value="M"},
            new OptionItem{ Text="Female", Value="F"},
            new OptionItem{ Text="Other",  Value="O"}
        };
        public ObservableCollection<OptionItem> FilterGenderOptions { get; } = new()
        {
            new OptionItem{ Text="All",    Value="" },
            new OptionItem{ Text="Male",   Value="M"},
            new OptionItem{ Text="Female", Value="F"},
            new OptionItem{ Text="Other",  Value="O"}
        };

        // ======= Filters =======
        private string _filterName = "";
        public string FilterName { get => _filterName; set { Set(ref _filterName, value); DebouncedReload(); } }

        private int? _filterDeptId;
        public int? FilterDeptId { get => _filterDeptId; set { Set(ref _filterDeptId, value); DebouncedReload(); } }

        private string _filterGender = "";
        public string FilterGender { get => _filterGender; set { Set(ref _filterGender, value); DebouncedReload(); } }

        private string _filterSalaryMinText = "";
        public string FilterSalaryMinText { get => _filterSalaryMinText; set { Set(ref _filterSalaryMinText, value); DebouncedReload(); } }

        private string _filterSalaryMaxText = "";
        public string FilterSalaryMaxText { get => _filterSalaryMaxText; set { Set(ref _filterSalaryMaxText, value); DebouncedReload(); } }

        private DateTime? _filterHireFrom;
        public DateTime? FilterHireFrom { get => _filterHireFrom; set { Set(ref _filterHireFrom, value); DebouncedReload(); } }

        private DateTime? _filterHireTo;
        public DateTime? FilterHireTo { get => _filterHireTo; set { Set(ref _filterHireTo, value); DebouncedReload(); } }

        // ======= Form (Edit) =======
        private int? _editingEmployeeId = null;

        private string _editFullName = "";
        public string EditFullName { get => _editFullName; set => Set(ref _editFullName, value); }

        private DateTime? _editDateOfBirth;
        public DateTime? EditDateOfBirth { get => _editDateOfBirth; set => Set(ref _editDateOfBirth, value); }

        private string _editGender = "";
        public string EditGender { get => _editGender; set => Set(ref _editGender, value); }

        private string _editAddress = "";
        public string EditAddress { get => _editAddress; set => Set(ref _editAddress, value); }

        private string _editPhone = "";
        public string EditPhone { get => _editPhone; set => Set(ref _editPhone, value); }

        private int? _editDepartmentId;
        public int? EditDepartmentId { get => _editDepartmentId; set => Set(ref _editDepartmentId, value); }

        private string _editPosition = "";
        public string EditPosition { get => _editPosition; set => Set(ref _editPosition, value); }

        // Text thay vì decimal để tránh converter phức tạp trong TextBox
        private string _editBaseSalaryText = "";
        public string EditBaseSalaryText { get => _editBaseSalaryText; set => Set(ref _editBaseSalaryText, value); }

        private DateTime? _editHireDate;
        public DateTime? EditHireDate { get => _editHireDate; set => Set(ref _editHireDate, value); }

        // Avatar
        private byte[]? _avatarBytes;
        public BitmapImage? AvatarPreviewImage
        {
            get => _avatarPreviewImage;
            private set => Set(ref _avatarPreviewImage, value);
        }
        private BitmapImage? _avatarPreviewImage;

        // Selected row
        private EmployeeRow? _selectedRow;
        public EmployeeRow? SelectedRow
        {
            get => _selectedRow;
            set { if (Set(ref _selectedRow, value)) _ = LoadRowToFormAsync(value); }
        }

        // ======= Commands =======
        public ICommand NewCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand UploadAvatarCommand { get; }

        public AccountManagerViewModel()
        {
            NewCommand = new RelayCommand(_ => New(), _ => true);
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => true);
            DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => _editingEmployeeId.HasValue);
            CancelCommand = new RelayCommand(_ => Cancel(), _ => true);
            UploadAvatarCommand = new RelayCommand(_ => UploadAvatar(), _ => true);

            _ = InitAsync();
        }

        // ======= Init =======
        private async Task InitAsync()
        {
            try
            {
                using var db = NewDb();
                var depts = await db.Departments.AsNoTracking()
                                  .OrderBy(d => d.DepartmentName)
                                  .ToListAsync();
                Departments.Clear();
                foreach (var d in depts) Departments.Add(d);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Load departments failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            await ReloadGridAsync();
        }

        // ======= Context helper =======
        private static ManagementEmployeeContext NewDb() => new ManagementEmployeeContext();

        // ======= Reload grid (debounced) =======
        private CancellationTokenSource? _reloadCts;
        private void DebouncedReload()
        {
            _reloadCts?.Cancel();
            _reloadCts = new CancellationTokenSource();
            var token = _reloadCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(250, token);
                    if (!token.IsCancellationRequested) await ReloadGridAsync();
                }
                catch (TaskCanceledException) { }
            }, token);
        }

        private static DateOnly? ToDateOnly(DateTime? dt) => dt.HasValue ? DateOnly.FromDateTime(dt.Value) : (DateOnly?)null;
        private static DateTime ToDateTime(DateOnly d) => new(d.Year, d.Month, d.Day);

        private static decimal? TryParseDecimal(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out var v)) return v;
            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out v)) return v;
            var compact = input.Replace(".", "").Replace(",", "");
            return decimal.TryParse(compact, NumberStyles.Number, CultureInfo.InvariantCulture, out v) ? v : null;
        }

        public async Task ReloadGridAsync()
        {
            try
            {
                string nameKey = (FilterName ?? "").Trim().ToLower();
                int? deptId = FilterDeptId;
                string gender = FilterGender ?? "";
                decimal? salaryMin = TryParseDecimal(FilterSalaryMinText ?? "");
                decimal? salaryMax = TryParseDecimal(FilterSalaryMaxText ?? "");
                DateOnly? hireFrom = ToDateOnly(FilterHireFrom);
                DateOnly? hireTo = ToDateOnly(FilterHireTo);

                using var db = NewDb();

                var q = from e in db.Employees.AsNoTracking()
                        join d in db.Departments.AsNoTracking() on e.DepartmentId equals d.DepartmentId
                        select new { e, d };

                if (!string.IsNullOrWhiteSpace(nameKey))
                    q = q.Where(x => x.e.FullName.ToLower().Contains(nameKey) || x.e.Position.ToLower().Contains(nameKey));
                if (deptId.HasValue)
                    q = q.Where(x => x.d.DepartmentId == deptId.Value);
                if (!string.IsNullOrEmpty(gender))
                    q = q.Where(x => x.e.Gender == gender);
                if (salaryMin.HasValue)
                    q = q.Where(x => x.e.BaseSalary >= salaryMin.Value);
                if (salaryMax.HasValue)
                    q = q.Where(x => x.e.BaseSalary <= salaryMax.Value);
                if (hireFrom.HasValue)
                    q = q.Where(x => x.e.HireDate >= hireFrom.Value);
                if (hireTo.HasValue)
                    q = q.Where(x => x.e.HireDate <= hireTo.Value);

                var list = await q
                    .OrderByDescending(x => x.e.HireDate)
                    .Select(x => new EmployeeRow
                    {
                        EmployeeId = x.e.EmployeeId,
                        FullName = x.e.FullName,
                        DepartmentName = x.d.DepartmentName,
                        Position = x.e.Position,
                        GenderText = x.e.Gender == "M" ? "Male" :
                                     x.e.Gender == "F" ? "Female" :
                                     x.e.Gender == "O" ? "Other" : "",
                        Phone = x.e.Phone,
                        BaseSalary = x.e.BaseSalary,
                        HireDate = ToDateTime(x.e.HireDate)
                    })
                    .ToListAsync();

                GridItems.Clear();
                foreach (var row in list) GridItems.Add(row);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Load failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ======= Load selected row to form =======
        private async Task LoadRowToFormAsync(EmployeeRow? row)
        {
            if (row == null) { New(); return; }

            try
            {
                using var db = NewDb();
                var emp = await db.Employees.AsNoTracking()
                              .FirstOrDefaultAsync(x => x.EmployeeId == row.EmployeeId);
                if (emp == null) { New(); return; }

                _editingEmployeeId = emp.EmployeeId;
                EditFullName = emp.FullName;
                EditDateOfBirth = ToDateTime(emp.DateOfBirth);
                EditGender = emp.Gender ?? "";
                EditAddress = emp.Address ?? "";
                EditPhone = emp.Phone ?? "";
                EditDepartmentId = emp.DepartmentId;
                EditPosition = emp.Position ?? "";
                EditBaseSalaryText = emp.BaseSalary.ToString("N0", CultureInfo.CurrentCulture);
                EditHireDate = ToDateTime(emp.HireDate);

                _avatarBytes = emp.AvatarBlob;
                AvatarPreviewImage = BytesToBitmap(_avatarBytes);
                OnPropertyChanged(nameof(DeleteCommand)); // CanExecute change hint if needed
            }
            catch (Exception ex)
            {
                MessageBox.Show("Load detail failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ======= Commands impl =======
        private void New()
        {
            _editingEmployeeId = null;
            EditFullName = "";
            EditDateOfBirth = null;
            EditGender = "";
            EditAddress = "";
            EditPhone = "";
            EditDepartmentId = null;
            EditPosition = "";
            EditBaseSalaryText = "";
            EditHireDate = null;
            _avatarBytes = null;
            AvatarPreviewImage = null;
            SelectedRow = null;
        }

        private void Cancel() => New();

        private async Task SaveAsync()
        {
            // Validate + map
            var (ok, msg, entity) = BuildEmployeeFromForm();
            if (!ok || entity == null)
            {
                MessageBox.Show(msg, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var db = NewDb();

                if (_editingEmployeeId.HasValue)
                {
                    var emp = await db.Employees.FirstOrDefaultAsync(x => x.EmployeeId == _editingEmployeeId.Value);
                    if (emp == null) { MessageBox.Show("Employee not found.", "Error"); return; }

                    emp.FullName = entity.FullName;
                    emp.DateOfBirth = entity.DateOfBirth;
                    emp.Gender = entity.Gender;
                    emp.Address = entity.Address;
                    emp.Phone = entity.Phone;
                    emp.DepartmentId = entity.DepartmentId;
                    emp.Position = entity.Position;
                    emp.BaseSalary = entity.BaseSalary;
                    emp.HireDate = entity.HireDate;
                    if (_avatarBytes != null) emp.AvatarBlob = _avatarBytes;

                    await db.SaveChangesAsync();
                    MessageBox.Show("Updated successfully.", "Info");
                }
                else
                {
                    entity.AvatarBlob = _avatarBytes;
                    db.Employees.Add(entity);
                    await db.SaveChangesAsync();
                    _editingEmployeeId = entity.EmployeeId;
                    MessageBox.Show("Created successfully.", "Info");
                }

                await ReloadGridAsync();

                // chọn lại dòng vừa lưu (nếu có)
                if (_editingEmployeeId.HasValue)
                {
                    var found = GridItems.FirstOrDefault(x => x.EmployeeId == _editingEmployeeId.Value);
                    if (found != null) SelectedRow = found;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteAsync()
        {
            if (!_editingEmployeeId.HasValue)
            {
                MessageBox.Show("Select an employee first.", "Info");
                return;
            }
            if (MessageBox.Show("Delete this employee?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                using var db = NewDb();
                var emp = await db.Employees.FirstOrDefaultAsync(x => x.EmployeeId == _editingEmployeeId.Value);
                if (emp == null) { MessageBox.Show("Employee not found.", "Error"); return; }

                db.Employees.Remove(emp);
                await db.SaveChangesAsync();

                MessageBox.Show("Deleted.", "Info");
                New();
                await ReloadGridAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UploadAvatar()
        {
            try
            {
                var ofd = new OpenFileDialog
                {
                    Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
                    Multiselect = false
                };
                if (ofd.ShowDialog() == true)
                {
                    var info = new FileInfo(ofd.FileName);
                    if (info.Length > 2 * 1024 * 1024)
                    {
                        MessageBox.Show("Please choose an image ≤ 2MB.", "Warning");
                        return;
                    }

                    _avatarBytes = File.ReadAllBytes(ofd.FileName);
                    AvatarPreviewImage = BytesToBitmap(_avatarBytes);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Upload failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static BitmapImage? BytesToBitmap(byte[]? bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;
            using var ms = new MemoryStream(bytes);
            var img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.StreamSource = ms;
            img.EndInit();
            img.Freeze();
            return img;
        }

        private (bool ok, string msg, Employee? entity) BuildEmployeeFromForm()
        {
            string fullName = (EditFullName ?? "").Trim();
            if (fullName.Length < 2) return (false, "Full name is required (≥2 chars).", null);

            var dob = EditDateOfBirth;
            if (!dob.HasValue) return (false, "Date of birth is required.", null);

            string gender = EditGender ?? "";
            if (string.IsNullOrEmpty(gender)) return (false, "Please select gender.", null);

            if (!EditDepartmentId.HasValue) return (false, "Please select department.", null);

            string position = (EditPosition ?? "").Trim();
            if (string.IsNullOrEmpty(position)) return (false, "Position is required.", null);

            var baseSalaryOpt = TryParseDecimal(EditBaseSalaryText ?? "");
            if (!baseSalaryOpt.HasValue)
                return (false, "Base salary is invalid.", null);
            decimal baseSalary = baseSalaryOpt.Value;


            var hireDate = EditHireDate;
            if (!hireDate.HasValue) return (false, "Hire date is required.", null);

            var emp = new Employee
            {
                FullName = fullName,
                DateOfBirth = DateOnly.FromDateTime(dob.Value.Date),
                Gender = gender,
                Address = (EditAddress ?? "").Trim(),
                Phone = (EditPhone ?? "").Trim(),
                DepartmentId = EditDepartmentId.Value,
                Position = position,
                BaseSalary = baseSalary,
                HireDate = DateOnly.FromDateTime(hireDate.Value.Date)
            };
            return (true, "", emp);
        }

        // ======= INotifyPropertyChanged =======
        public event PropertyChangedEventHandler? PropertyChanged;
        private bool Set<T>(ref T field, T value, [CallerMemberName] string? prop = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
            return true;
        }
        private void OnPropertyChanged([CallerMemberName] string? prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
