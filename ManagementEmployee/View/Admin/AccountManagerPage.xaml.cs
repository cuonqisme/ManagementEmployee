using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ManagementEmployee.Models;

namespace ManagementEmployee.View.Admin
{
    public partial class AccountManagerPage : Page
    {
        private int? _editingEmployeeId = null;
        private byte[] _avatarBytesBuffer = null;
        private bool _pastingHandlersAttached = false;
        private CancellationTokenSource _reloadCts;

        public AccountManagerPage()
        {
            InitializeComponent();
            Loaded += OnLoadedSafe;
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _reloadCts?.Cancel();
            _reloadCts?.Dispose();
            _reloadCts = null;
        }

        private ManagementEmployeeContext NewDb() => new ManagementEmployeeContext();

        private static DateOnly? ToDateOnly(DateTime? dt) =>
            dt.HasValue ? DateOnly.FromDateTime(dt.Value) : (DateOnly?)null;

        private static DateTime ToDateTime(DateOnly d) =>
            new DateTime(d.Year, d.Month, d.Day);

        private class EmployeeRow
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

        private async void OnLoadedSafe(object sender, RoutedEventArgs e)
        {
            AttachPastingHandlersOnce();

            using var db = NewDb();
            var depts = await db.Departments
                                .AsNoTracking()
                                .OrderBy(d => d.DepartmentName)
                                .ToListAsync();

            if (cbFilterDept != null)
            {
                cbFilterDept.Items.Clear();
                cbFilterDept.Items.Add(new ComboBoxItem { Content = "All departments", Tag = null, IsSelected = true });
                foreach (var d in depts)
                    cbFilterDept.Items.Add(new ComboBoxItem { Content = d.DepartmentName, Tag = d.DepartmentId });
            }

            if (cbDepartment != null)
            {
                cbDepartment.ItemsSource = depts;
                cbDepartment.DisplayMemberPath = "DepartmentName";
                cbDepartment.SelectedValuePath = "DepartmentId";
            }

            await ReloadGridAsync();
            ResetForm();
        }

        private void AttachPastingHandlersOnce()
        {
            if (_pastingHandlersAttached) return;
            if (txtBaseSalary != null) DataObject.AddPastingHandler(txtBaseSalary, OnPasteNumericOnly);
            if (txtSalaryMin != null) DataObject.AddPastingHandler(txtSalaryMin, OnPasteNumericOnly);
            if (txtSalaryMax != null) DataObject.AddPastingHandler(txtSalaryMax, OnPasteNumericOnly);
            _pastingHandlersAttached = true;
        }

        // ILTER + GRID 
        private async Task ReloadGridAsync()
        {
            var cts = new CancellationTokenSource();
            var old = Interlocked.Exchange(ref _reloadCts, cts);
            old?.Cancel();
            old?.Dispose();
            var ct = cts.Token;

            string nameKey = (txtFilterName?.Text ?? "").Trim().ToLower();
            int? filterDeptId = GetSelectedDeptId(cbFilterDept);
            string genderKey = GetSelectedGender(cbFilterGender);
            decimal? salaryMin = TryParseDecimal(txtSalaryMin?.Text ?? "");
            decimal? salaryMax = TryParseDecimal(txtSalaryMax?.Text ?? "");
            DateOnly? hireFrom = ToDateOnly(dpHireFrom?.SelectedDate);
            DateOnly? hireTo = ToDateOnly(dpHireTo?.SelectedDate);

            try
            {
                using var db = NewDb();

                var q = from e in db.Employees.AsNoTracking()
                        join d in db.Departments.AsNoTracking() on e.DepartmentId equals d.DepartmentId
                        select new { e, d };

                if (!string.IsNullOrWhiteSpace(nameKey))
                    q = q.Where(x => x.e.FullName.ToLower().Contains(nameKey) || x.e.Position.ToLower().Contains(nameKey));

                if (filterDeptId.HasValue)
                    q = q.Where(x => x.d.DepartmentId == filterDeptId.Value);

                if (!string.IsNullOrEmpty(genderKey))
                    q = q.Where(x => x.e.Gender == genderKey);

                if (salaryMin.HasValue)
                    q = q.Where(x => x.e.BaseSalary >= salaryMin.Value);

                if (salaryMax.HasValue)
                    q = q.Where(x => x.e.BaseSalary <= salaryMax.Value);

                if (hireFrom.HasValue)
                    q = q.Where(x => x.e.HireDate >= hireFrom.Value);

                if (hireTo.HasValue)
                    q = q.Where(x => x.e.HireDate <= hireTo.Value);

                var rows = await q
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
                    .ToListAsync(ct);

                if (!ct.IsCancellationRequested && dgEmployees != null)
                    dgEmployees.ItemsSource = rows;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                MessageBox.Show("Load failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static int? GetSelectedDeptId(ComboBox cb)
        {
            if (cb?.SelectedItem is ComboBoxItem item && item.Tag != null &&
                int.TryParse(item.Tag.ToString(), out int id))
                return id;
            return null;
        }

        private static string GetSelectedGender(ComboBox cb)
        {
            if (cb?.SelectedItem is ComboBoxItem item)
                return item.Tag?.ToString() ?? "";
            return "";
        }

        private static decimal? TryParseDecimal(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out var v)) return v;
            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out v)) return v;
            var compact = input.Replace(".", "").Replace(",", "");
            if (decimal.TryParse(compact, NumberStyles.Number, CultureInfo.InvariantCulture, out v)) return v;
            return null;
        }

        private async void AnyFilterChanged(object sender, RoutedEventArgs e) => await ReloadGridAsync();

        private void NumericOnly(object sender, TextCompositionEventArgs e) => e.Handled = !IsNumericInput(e.Text);
        private void OnPasteNumericOnly(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!IsNumericInput(text)) e.CancelCommand();
            }
            else e.CancelCommand();
        }
        private static bool IsNumericInput(string text)
        {
            foreach (char c in text) if (!char.IsDigit(c) && c != '.' && c != ',') return false;
            return true;
        }


        private async void dgEmployees_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgEmployees?.SelectedItem is not EmployeeRow row) return;

            using var db = NewDb();
            var emp = await db.Employees
                              .AsNoTracking()
                              .FirstOrDefaultAsync(x => x.EmployeeId == row.EmployeeId);
            if (emp == null) return;

            _editingEmployeeId = emp.EmployeeId;

            if (txtFullName != null) txtFullName.Text = emp.FullName;
            if (dpDob != null) dpDob.SelectedDate = ToDateTime(emp.DateOfBirth);
            SelectGender(cbGender, emp.Gender);
            if (txtAddress != null) txtAddress.Text = emp.Address;
            if (txtPhone != null) txtPhone.Text = emp.Phone;
            if (cbDepartment != null) cbDepartment.SelectedValue = emp.DepartmentId;
            if (txtPosition != null) txtPosition.Text = emp.Position;
            if (txtBaseSalary != null) txtBaseSalary.Text = emp.BaseSalary.ToString("N0", CultureInfo.CurrentCulture);
            if (dpHireDate != null) dpHireDate.SelectedDate = ToDateTime(emp.HireDate);

            _avatarBytesBuffer = emp.AvatarBlob;
            LoadImageFromBytes(_avatarBytesBuffer);
        }

        private static void SelectGender(ComboBox cb, string tag)
        {
            if (cb == null) return;
            for (int i = 0; i < cb.Items.Count; i++)
            {
                if (cb.Items[i] is ComboBoxItem cbi && (cbi.Tag?.ToString() ?? "") == (tag ?? ""))
                { cb.SelectedIndex = i; return; }
            }
            cb.SelectedIndex = -1;
        }

        private void LoadImageFromBytes(byte[] bytes)
        {
            if (imgAvatar != null) imgAvatar.Source = null;
            if (bytes == null || bytes.Length == 0) return;
            using var ms = new MemoryStream(bytes);
            var img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.StreamSource = ms;
            img.EndInit();
            img.Freeze();
            if (imgAvatar != null) imgAvatar.Source = img;
        }

        // tao employee
        private async void btnNew_Click(object sender, RoutedEventArgs e)
        {
            if (IsFormEmpty())
            {
                _editingEmployeeId = null;
                ResetForm();
                txtFullName?.Focus();
                return;
            }

            await SaveEmployeeAsync(forceCreate: true, resetAfter: true);
        }

        // SAVE
        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            await SaveEmployeeAsync(forceCreate: false, resetAfter: false);
        }

        private async Task SaveEmployeeAsync(bool forceCreate, bool resetAfter)
        {
            var (ok, msg, entity) = BuildEmployeeFromForm();
            if (!ok)
            {
                MessageBox.Show(msg, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var db = NewDb();

                if (!forceCreate && _editingEmployeeId.HasValue)
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
                    if (_avatarBytesBuffer != null) emp.AvatarBlob = _avatarBytesBuffer;

                    await db.SaveChangesAsync();
                    MessageBox.Show("Updated successfully.", "Info");
                }
                else
                {
                    entity.AvatarBlob = _avatarBytesBuffer;
                    db.Employees.Add(entity);
                    await db.SaveChangesAsync();
                    MessageBox.Show("Created successfully.", "Info");
                    _editingEmployeeId = entity.EmployeeId;
                }

                await ReloadGridAsync();

                if (resetAfter)
                {
                    ResetForm();
                    _editingEmployeeId = null;
                    dgEmployees.SelectedItem = null;
                    txtFullName?.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
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
                ResetForm();
                _editingEmployeeId = null;
                dgEmployees.SelectedItem = null;
                await ReloadGridAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
            dgEmployees.SelectedItem = null;
            _editingEmployeeId = null;
            txtFullName?.Focus();
        }

        private void btnUploadAvatar_Click(object sender, RoutedEventArgs e)
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

                _avatarBytesBuffer = File.ReadAllBytes(ofd.FileName);
                LoadImageFromBytes(_avatarBytesBuffer);
            }
        }

        private (bool ok, string msg, Employee entity) BuildEmployeeFromForm()
        {
            string fullName = (txtFullName?.Text ?? "").Trim();
            if (fullName.Length < 2) return (false, "Full name is required (≥2 chars).", null);

            var dob = ToDateOnly(dpDob?.SelectedDate);
            if (!dob.HasValue) return (false, "Date of birth is required.", null);

            string gender = (cbGender?.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (string.IsNullOrEmpty(gender)) return (false, "Please select gender.", null);

            if (cbDepartment?.SelectedValue is not int deptId) return (false, "Please select department.", null);

            string position = (txtPosition?.Text ?? "").Trim();
            if (string.IsNullOrEmpty(position)) return (false, "Position is required.", null);

            if (!TryParseSalary((txtBaseSalary?.Text ?? ""), out decimal baseSalary))
                return (false, "Base salary is invalid.", null);

            var hireDate = ToDateOnly(dpHireDate?.SelectedDate);
            if (!hireDate.HasValue) return (false, "Hire date is required.", null);

            var emp = new Employee
            {
                FullName = fullName,
                DateOfBirth = dob.Value,
                Gender = gender,
                Address = (txtAddress?.Text ?? "").Trim(),
                Phone = (txtPhone?.Text ?? "").Trim(),
                DepartmentId = deptId,
                Position = position,
                BaseSalary = baseSalary,
                HireDate = hireDate.Value
            };
            return (true, "", emp);
        }

        private static bool TryParseSalary(string input, out decimal value)
        {
            input = (input ?? "").Trim();
            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out value)) return true;
            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out value)) return true;
            var compact = input.Replace(".", "").Replace(",", "");
            return decimal.TryParse(compact, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        private bool IsFormEmpty()
        {
            bool empty =
                string.IsNullOrWhiteSpace(txtFullName?.Text) &&
                !dpDob?.SelectedDate.HasValue == true &&
                (cbGender?.SelectedIndex ?? -1) == -1 &&
                string.IsNullOrWhiteSpace(txtAddress?.Text) &&
                string.IsNullOrWhiteSpace(txtPhone?.Text) &&
                (cbDepartment?.SelectedIndex ?? -1) == -1 &&
                string.IsNullOrWhiteSpace(txtPosition?.Text) &&
                string.IsNullOrWhiteSpace(txtBaseSalary?.Text) &&
                !dpHireDate?.SelectedDate.HasValue == true &&
                (_avatarBytesBuffer == null || _avatarBytesBuffer.Length == 0);
            return empty;
        }

        private void ResetForm()
        {
            if (txtFullName != null) txtFullName.Text = "";
            if (dpDob != null) dpDob.SelectedDate = null;
            if (cbGender != null) cbGender.SelectedIndex = -1;
            if (txtAddress != null) txtAddress.Text = "";
            if (txtPhone != null) txtPhone.Text = "";
            if (cbDepartment != null) cbDepartment.SelectedIndex = -1;
            if (txtPosition != null) txtPosition.Text = "";
            if (txtBaseSalary != null) txtBaseSalary.Text = "";
            if (dpHireDate != null) dpHireDate.SelectedDate = null;
            _avatarBytesBuffer = null;
            if (imgAvatar != null) imgAvatar.Source = null;


            if (dgEmployees != null)
            {
                dgEmployees.SelectedItem = null;
                dgEmployees.SelectedIndex = -1;
            }
        }
    }
}
