using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ManagementEmployee.Models;
using Microsoft.EntityFrameworkCore;
using ManagementEmployee.ViewModels;

namespace ManagementEmployee.ViewModels.Admin
{
    public sealed class PayrollManagerViewModel : ViewModelBase
    {
        // ==== DTO hiển thị lưới ====
        public sealed class PayrollRow
        {
            public int PayrollId { get; set; }
            public string EmployeeName { get; set; }
            public string DepartmentName { get; set; }
            public string PeriodText { get; set; }
            public decimal BasicSalary { get; set; }
            public decimal OvertimePay { get; set; }
            public decimal TotalAllowance { get; set; }
            public decimal TotalBonus { get; set; }
            public decimal TotalPenalty { get; set; }
            public decimal TotalDeduction { get; set; }
            public decimal Gross { get; set; }
            public decimal Net { get; set; }
        }

        public sealed class EmpLite
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int DepartmentId { get; set; }
            public decimal BaseSalary { get; set; }
            public override string ToString() => Name;
        }

        public sealed class AdjRow
        {
            public int AdjustmentId { get; set; }
            public string AdjType { get; set; } // ALLOWANCE/BONUS/PENALTY/DEDUCTION/OVERTIME
            public decimal Amount { get; set; }
            public string Description { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public sealed class OptionItem<T>
        {
            public string Display { get; set; }
            public T Value { get; set; }
            public override string ToString() => Display;
        }

        // ==== Collections ====
        public ObservableCollection<PayrollRow> Payrolls { get; } = new();
        public ObservableCollection<OptionItem<int?>> DeptOptions { get; } = new();
        public ObservableCollection<OptionItem<int?>> YearOptions { get; } = new();
        public ObservableCollection<OptionItem<int?>> MonthOptions { get; } = new();
        public ObservableCollection<EmpLite> Employees { get; } = new();
        public ObservableCollection<AdjRow> Adjustments { get; } = new();

        // ==== Filter (Header) ====
        private string _filterName;
        public string FilterName
        {
            get => _filterName;
            set
            {
                if (SetProperty(ref _filterName, value))
                    RefreshPreviewAllowed();
            }
        }


        private OptionItem<int?> _filterDept;
        public OptionItem<int?> FilterDept { get => _filterDept; set => SetProperty(ref _filterDept, value); }

        private OptionItem<int?> _filterYear;
        public OptionItem<int?> FilterYear { get => _filterYear; set => SetProperty(ref _filterYear, value); }

        private OptionItem<int?> _filterMonth;
        public OptionItem<int?> FilterMonth { get => _filterMonth; set => SetProperty(ref _filterMonth, value); }

        // ==== Selection on grid ====
        private PayrollRow _selectedPayroll;
        public PayrollRow SelectedPayroll
        {
            get => _selectedPayroll;
            set { if (SetProperty(ref _selectedPayroll, value)) _ = LoadPayrollDetailAsync(value?.PayrollId); }
        }

        // ==== Editor (form) ====
        private EmpLite _selectedEmployee;
        public EmpLite SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                if (SetProperty(ref _selectedEmployee, value))
                {
                    _ = UpdateEmpDeptTextAsync(value?.DepartmentId ?? 0);
                    if (EditingPayrollId == null && string.IsNullOrWhiteSpace(BasicText) && value != null)
                    {
                        BasicText = value.BaseSalary.ToString("N0", CultureInfo.CurrentCulture);
                    }
                }
            }
        }

        private string _empDeptText;
        public string EmpDeptText { get => _empDeptText; set => SetProperty(ref _empDeptText, value); }

        private OptionItem<int?> _editorYear;
        public OptionItem<int?> EditorYear { get => _editorYear; set => SetProperty(ref _editorYear, value); }

        private OptionItem<int?> _editorMonth;
        public OptionItem<int?> EditorMonth { get => _editorMonth; set => SetProperty(ref _editorMonth, value); }

        // Các ô số (bind kiểu string để nhập thoải mái; VM tự parse)
        private string _basicText; public string BasicText
        {
            get => _basicText;
            set { if (SetProperty(ref _basicText, value)) RecalcPreview(); }
        }
        private string _otText; public string OTText
        {
            get => _otText;
            set { if (SetProperty(ref _otText, value)) RecalcPreview(); }
        }
        private string _allowanceText; public string AllowanceText
        {
            get => _allowanceText;
            set { if (SetProperty(ref _allowanceText, value)) RecalcPreview(); }
        }
        private string _bonusText; public string BonusText
        {
            get => _bonusText;
            set { if (SetProperty(ref _bonusText, value)) RecalcPreview(); }
        }
        private string _penaltyText; public string PenaltyText
        {
            get => _penaltyText;
            set { if (SetProperty(ref _penaltyText, value)) RecalcPreview(); }
        }
        private string _deductionText; public string DeductionText
        {
            get => _deductionText;
            set { if (SetProperty(ref _deductionText, value)) RecalcPreview(); }
        }

        private DateTime? _payDate;
        public DateTime? PayDate { get => _payDate; set => SetProperty(ref _payDate, value); }

        private string _grossPreview = "—";
        public string GrossPreview { get => _grossPreview; set => SetProperty(ref _grossPreview, value); }

        private string _netPreview = "—";
        public string NetPreview { get => _netPreview; set => SetProperty(ref _netPreview, value); }

        // ==== Adjustments (editor) ====
        private string _selectedAdjType;
        public string SelectedAdjType { get => _selectedAdjType; set => SetProperty(ref _selectedAdjType, value); }

        private string _adjAmountText;
        public string AdjAmountText { get => _adjAmountText; set => SetProperty(ref _adjAmountText, value); }

        private string _adjDesc;
        public string AdjDesc { get => _adjDesc; set => SetProperty(ref _adjDesc, value); }

        private AdjRow _selectedAdjustment;
        public AdjRow SelectedAdjustment { get => _selectedAdjustment; set => SetProperty(ref _selectedAdjustment, value); }

        // ==== Command ====
        public AsyncRelayCommand LoadedCommand { get; }
        public AsyncRelayCommand SearchCommand { get; }
        public AsyncRelayCommand RefreshCommand { get; }
        public RelayCommand NewCommand { get; }
        public AsyncRelayCommand SaveCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }
        public AsyncRelayCommand AddAdjCommand { get; }
        public AsyncRelayCommand RemoveAdjCommand { get; }
        public AsyncRelayCommand RecalcFromAdjCommand { get; }

        // ==== Internal ====
        private int? EditingPayrollId { get; set; }

        public PayrollManagerViewModel()
        {
            LoadedCommand = new AsyncRelayCommand(_ => InitializeAsync());
            SearchCommand = new AsyncRelayCommand(_ => ReloadGridAsync());
            RefreshCommand = new AsyncRelayCommand(_ => ReloadGridAsync());
            NewCommand = new RelayCommand(_ => ResetForm());
            SaveCommand = new AsyncRelayCommand(_ => SaveAsync());
            DeleteCommand = new AsyncRelayCommand(_ => DeleteAsync());
            AddAdjCommand = new AsyncRelayCommand(_ => AddAdjustmentAsync());
            RemoveAdjCommand = new AsyncRelayCommand(_ => RemoveAdjustmentAsync());
            RecalcFromAdjCommand = new AsyncRelayCommand(_ => RecalcTotalsFromAdjustmentsAsyncAndReload());
        }

        private ManagementEmployeeContext NewDb() => new ManagementEmployeeContext();

        // ===== Initialize =====
        private async Task InitializeAsync()
        {
            await LoadFilterOptionsAsync();
            await LoadEmployeesAsync();
            await ReloadGridAsync();
            ResetForm();
        }

        private async Task LoadFilterOptionsAsync()
        {
            DeptOptions.Clear();
            YearOptions.Clear();
            MonthOptions.Clear();

            DeptOptions.Add(new OptionItem<int?> { Display = "All departments", Value = null });
            using (var db = NewDb())
            {
                var depts = await db.Departments.AsNoTracking().OrderBy(d => d.DepartmentName).ToListAsync();
                foreach (var d in depts)
                    DeptOptions.Add(new OptionItem<int?> { Display = d.DepartmentName, Value = d.DepartmentId });
            }
            FilterDept = DeptOptions.FirstOrDefault();

            int nowY = DateTime.Now.Year;
            YearOptions.Add(new OptionItem<int?> { Display = "All years", Value = null });
            for (int y = nowY - 2; y <= nowY + 1; y++)
                YearOptions.Add(new OptionItem<int?> { Display = y.ToString(), Value = y });
            FilterYear = YearOptions.FirstOrDefault();

            MonthOptions.Add(new OptionItem<int?> { Display = "All months", Value = null });
            for (int m = 1; m <= 12; m++)
                MonthOptions.Add(new OptionItem<int?> { Display = m.ToString("00"), Value = m });
            FilterMonth = MonthOptions.FirstOrDefault();

            // Editor year/month mặc định = hiện tại
            EditorYear = YearOptions.FirstOrDefault(o => o.Value == nowY) ?? YearOptions.First();
            EditorMonth = MonthOptions.FirstOrDefault(o => o.Value == DateTime.Now.Month) ?? MonthOptions.First();
        }

        private async Task LoadEmployeesAsync()
        {
            Employees.Clear();
            using var db = NewDb();
            var emps = await db.Employees.AsNoTracking()
                          .Where(e => e.IsActive)
                          .OrderBy(e => e.FullName)
                          .Select(e => new EmpLite
                          {
                              Id = e.EmployeeId,
                              Name = e.FullName,
                              DepartmentId = e.DepartmentId,
                              BaseSalary = e.BaseSalary
                          })
                          .ToListAsync();
            foreach (var e in emps) Employees.Add(e);
        }

        // ===== Grid load =====
        private async Task ReloadGridAsync()
        {
            string kw = (FilterName ?? "").Trim().ToLower();
            int? deptId = FilterDept?.Value;
            int? year = FilterYear?.Value;
            int? month = FilterMonth?.Value;

            using var db = NewDb();
            var q = from p in db.Payrolls.AsNoTracking()
                    join e in db.Employees.AsNoTracking() on p.EmployeeId equals e.EmployeeId
                    join d in db.Departments.AsNoTracking() on e.DepartmentId equals d.DepartmentId
                    select new { p, e, d };

            if (!string.IsNullOrWhiteSpace(kw))
                q = q.Where(x => x.e.FullName.ToLower().Contains(kw) || x.e.Position.ToLower().Contains(kw));
            if (deptId.HasValue)
                q = q.Where(x => x.d.DepartmentId == deptId.Value);
            if (year.HasValue)
                q = q.Where(x => x.p.PeriodYear == year.Value);
            if (month.HasValue)
                q = q.Where(x => x.p.PeriodMonth == month.Value);

            var list = await q
                .OrderByDescending(x => x.p.PeriodYear)
                .ThenByDescending(x => x.p.PeriodMonth)
                .ThenBy(x => x.e.FullName)
                .Select(x => new PayrollRow
                {
                    PayrollId = x.p.PayrollId,
                    EmployeeName = x.e.FullName,
                    DepartmentName = x.d.DepartmentName,
                    PeriodText = $"{x.p.PeriodYear}-{(x.p.PeriodMonth < 10 ? "0" : "")}{x.p.PeriodMonth}",
                    BasicSalary = x.p.BasicSalary,
                    OvertimePay = x.p.OvertimePay,
                    TotalAllowance = x.p.TotalAllowance,
                    TotalBonus = x.p.TotalBonus,
                    TotalPenalty = x.p.TotalPenalty,
                    TotalDeduction = x.p.TotalDeduction,
                    Gross = x.p.Gross ?? 0m,
                    Net = x.p.Net ?? 0m
                })
                .ToListAsync();

            Payrolls.Clear();
            foreach (var r in list) Payrolls.Add(r);

            // Giữ lại chọn nếu đang edit
            if (EditingPayrollId.HasValue)
                SelectedPayroll = Payrolls.FirstOrDefault(p => p.PayrollId == EditingPayrollId.Value);
        }

        // ===== Load detail =====
        private async Task LoadPayrollDetailAsync(int? payrollId)
        {
            ResetForm(); // clear trước
            if (!payrollId.HasValue) return;

            using var db = NewDb();
            var p = await db.Payrolls.FirstOrDefaultAsync(x => x.PayrollId == payrollId.Value);
            if (p == null) return;

            EditingPayrollId = p.PayrollId;

            // employee
            SelectedEmployee = Employees.FirstOrDefault(x => x.Id == p.EmployeeId);
            // year/month
            EditorYear = YearOptions.FirstOrDefault(o => o.Value == (int)p.PeriodYear) ?? EditorYear;
            EditorMonth = MonthOptions.FirstOrDefault(o => o.Value == (int)p.PeriodMonth) ?? EditorMonth;

            // numbers
            BasicText = FormatN0(p.BasicSalary);
            OTText = FormatN0(p.OvertimePay);
            AllowanceText = FormatN0(p.TotalAllowance);
            BonusText = FormatN0(p.TotalBonus);
            PenaltyText = FormatN0(p.TotalPenalty);
            DeductionText = FormatN0(p.TotalDeduction);

            PayDate = ReadDateProperty(p, "PayDate"); // DateOnly? hay DateTime? -> tự dò

            GrossPreview = FormatN0(p.Gross ?? 0m);
            NetPreview = FormatN0(p.Net ?? 0m);

            // adjustments
            Adjustments.Clear();
            var adj = await db.PayrollAdjustments.AsNoTracking()
                        .Where(a => a.PayrollId == p.PayrollId)
                        .OrderByDescending(a => a.CreatedAt)
                        .Select(a => new AdjRow
                        {
                            AdjustmentId = a.AdjustmentId,
                            AdjType = a.AdjType,
                            Amount = a.Amount,
                            Description = a.Description,
                            CreatedAt = a.CreatedAt
                        })
                        .ToListAsync();
            foreach (var a in adj) Adjustments.Add(a);
        }

        // ===== Save/Delete =====
        private async Task SaveAsync()
        {
            if (SelectedEmployee == null) { Notify("Select employee."); return; }
            if (EditorYear?.Value == null || EditorMonth?.Value == null)
            { Notify("Select year & month."); return; }

            if (!TryParseDec(BasicText, out var basic)) { Notify("Invalid Basic Salary."); return; }
            var ot = ParseOrZero(OTText);
            var allow = ParseOrZero(AllowanceText);
            var bonus = ParseOrZero(BonusText);
            var penalty = ParseOrZero(PenaltyText);
            var deduction = ParseOrZero(DeductionText);

            using var db = NewDb();

            // duplicate check
            var existed = await db.Payrolls
                .FirstOrDefaultAsync(x => x.EmployeeId == SelectedEmployee.Id
                                       && x.PeriodYear == (short)EditorYear.Value.Value
                                       && x.PeriodMonth == (byte)EditorMonth.Value.Value);

            if (EditingPayrollId.HasValue)
            {
                var p = await db.Payrolls.FirstOrDefaultAsync(x => x.PayrollId == EditingPayrollId.Value);
                if (p == null) { Notify("Payroll not found."); return; }

                if (existed != null && existed.PayrollId != p.PayrollId)
                { Notify("This employee already has a payroll for selected period."); return; }

                p.EmployeeId = SelectedEmployee.Id;
                p.PeriodYear = (short)EditorYear.Value.Value;
                p.PeriodMonth = (byte)EditorMonth.Value.Value;
                p.BasicSalary = basic;
                p.OvertimePay = ot;
                p.TotalAllowance = allow;
                p.TotalBonus = bonus;
                p.TotalPenalty = penalty;
                p.TotalDeduction = deduction;
                WriteDateProperty(p, "PayDate", PayDate);

                await db.SaveChangesAsync();
                Notify("Updated.");
            }
            else
            {
                if (existed != null)
                { Notify("This employee already has a payroll for selected period."); return; }

                var p = new Payroll
                {
                    EmployeeId = SelectedEmployee.Id,
                    PeriodYear = (short)EditorYear.Value.Value,
                    PeriodMonth = (byte)EditorMonth.Value.Value,
                    BasicSalary = basic,
                    OvertimePay = ot,
                    TotalAllowance = allow,
                    TotalBonus = bonus,
                    TotalPenalty = penalty,
                    TotalDeduction = deduction
                };
                WriteDateProperty(p, "PayDate", PayDate);

                db.Payrolls.Add(p);
                await db.SaveChangesAsync();

                EditingPayrollId = p.PayrollId;
                Notify("Created.");
            }

            await ReloadGridAsync();
        }

        private async Task DeleteAsync()
        {
            if (!EditingPayrollId.HasValue && SelectedPayroll == null)
            { Notify("Select a payroll first."); return; }

            int id = EditingPayrollId ?? SelectedPayroll.PayrollId;

            using var db = NewDb();
            var p = await db.Payrolls.FirstOrDefaultAsync(x => x.PayrollId == id);
            if (p == null) { Notify("Payroll not found."); return; }

            var adjs = await db.PayrollAdjustments.Where(a => a.PayrollId == id).ToListAsync();
            if (adjs.Count > 0) db.PayrollAdjustments.RemoveRange(adjs);
            db.Payrolls.Remove(p);
            await db.SaveChangesAsync();

            EditingPayrollId = null;
            ResetForm();
            await ReloadGridAsync();
            Notify("Deleted.");
        }

        // ===== Adjustments =====
        private async Task AddAdjustmentAsync()
        {
            if (!EditingPayrollId.HasValue) { Notify("Select or save a payroll first."); return; }
            var type = (SelectedAdjType ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(type)) { Notify("Select adjustment type."); return; }
            if (!TryParseDec(AdjAmountText, out var amount)) { Notify("Invalid amount."); return; }
            var desc = (AdjDesc ?? "").Trim();

            using var db = NewDb();
            db.PayrollAdjustments.Add(new PayrollAdjustment
            {
                PayrollId = EditingPayrollId.Value,
                AdjType = type,
                Amount = amount,
                Description = desc,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            await RecalcTotalsFromAdjustmentsAsync(EditingPayrollId.Value);
            await LoadPayrollDetailAsync(EditingPayrollId.Value);
            Notify("Adjustment added & totals recalculated.");
        }

        private async Task RemoveAdjustmentAsync()
        {
            if (!EditingPayrollId.HasValue) { Notify("Select a payroll first."); return; }
            if (SelectedAdjustment == null) { Notify("Select an adjustment row."); return; }

            using var db = NewDb();
            var adj = await db.PayrollAdjustments
                              .FirstOrDefaultAsync(a => a.AdjustmentId == SelectedAdjustment.AdjustmentId);
            if (adj == null) { Notify("Adjustment not found."); return; }

            db.PayrollAdjustments.Remove(adj);
            await db.SaveChangesAsync();

            await RecalcTotalsFromAdjustmentsAsync(EditingPayrollId.Value);
            await LoadPayrollDetailAsync(EditingPayrollId.Value);
            Notify("Adjustment removed & totals recalculated.");
        }

        private async Task RecalcTotalsFromAdjustmentsAsyncAndReload()
        {
            if (!EditingPayrollId.HasValue) { Notify("Select a payroll first."); return; }
            await RecalcTotalsFromAdjustmentsAsync(EditingPayrollId.Value);
            await LoadPayrollDetailAsync(EditingPayrollId.Value);
            Notify("Recalculated from adjustments.");
        }

        private static async Task RecalcTotalsFromAdjustmentsAsync(int payrollId)
        {
            using var db = new ManagementEmployeeContext();

            var groups = await db.PayrollAdjustments.AsNoTracking()
                             .Where(a => a.PayrollId == payrollId)
                             .GroupBy(a => a.AdjType)
                             .Select(g => new { Type = g.Key, Sum = g.Sum(x => x.Amount) })
                             .ToListAsync();

            decimal sumAllow = groups.Where(g => g.Type == "ALLOWANCE").Sum(g => g.Sum);
            decimal sumBonus = groups.Where(g => g.Type == "BONUS").Sum(g => g.Sum);
            decimal sumPenalty = groups.Where(g => g.Type == "PENALTY").Sum(g => g.Sum);
            decimal sumDeduct = groups.Where(g => g.Type == "DEDUCTION").Sum(g => g.Sum);
            decimal sumOT = groups.Where(g => g.Type == "OVERTIME").Sum(g => g.Sum);

            var p = await db.Payrolls.FirstOrDefaultAsync(x => x.PayrollId == payrollId);
            if (p != null)
            {
                p.TotalAllowance = sumAllow;
                p.TotalBonus = sumBonus;
                p.TotalPenalty = sumPenalty;
                p.TotalDeduction = sumDeduct;
                p.OvertimePay = sumOT;
                await db.SaveChangesAsync();
            }
        }

        // ===== Helpers =====
        private static bool TryParseDec(string input, out decimal value)
        {
            input = (input ?? "").Trim();
            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out value)) return true;
            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out value)) return true;
            var compact = input.Replace(".", "").Replace(",", "");
            return decimal.TryParse(compact, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }
        private static decimal ParseOrZero(string input) => TryParseDec(input, out var v) ? v : 0m;
        private static string FormatN0(decimal v) => v.ToString("N0", CultureInfo.CurrentCulture);

        private void RecalcPreview()
        {
            var basic = ParseOrZero(BasicText);
            var ot = ParseOrZero(OTText);
            var allow = ParseOrZero(AllowanceText);
            var bonus = ParseOrZero(BonusText);
            var pen = ParseOrZero(PenaltyText);
            var ded = ParseOrZero(DeductionText);

            var gross = basic + ot + allow + bonus - pen;
            var net = gross - ded;

            GrossPreview = FormatN0(gross);
            NetPreview = FormatN0(net);
        }

        private async Task UpdateEmpDeptTextAsync(int deptId)
        {
            if (deptId == 0) { EmpDeptText = "Dept: —"; return; }
            using var db = NewDb();
            var d = await db.Departments.AsNoTracking().FirstOrDefaultAsync(x => x.DepartmentId == deptId);
            EmpDeptText = d != null ? $"Dept: {d.DepartmentName}" : "Dept: —";
        }

        private void ResetForm()
        {
            EditingPayrollId = null;
            SelectedEmployee = null;
            EmpDeptText = "";
            // giữ EditorYear/Month như đã chọn
            BasicText = OTText = AllowanceText = BonusText = PenaltyText = DeductionText = "";
            PayDate = null;
            GrossPreview = NetPreview = "—";
            Adjustments.Clear();
            SelectedAdjType = null;
            AdjAmountText = AdjDesc = null;
        }

        private void RefreshPreviewAllowed() => RecalcPreview();

        // ===== DateOnly/DateTime safe converters via reflection =====
        private static DateTime? ReadDateProperty(object obj, string propName)
        {
            if (obj == null) return null;
            var p = obj.GetType().GetProperty(propName);
            if (p == null) return null;
            var v = p.GetValue(obj);
            if (v == null) return null;
            var t = p.PropertyType;
            if (t == typeof(DateTime) || t == typeof(DateTime?)) return (DateTime?)v;
            if (t.Name.Contains("DateOnly"))
            {
                int year = (int)t.GetProperty("Year")!.GetValue(v)!;
                int month = (int)t.GetProperty("Month")!.GetValue(v)!;
                int day = (int)t.GetProperty("Day")!.GetValue(v)!;
                return new DateTime(year, month, day);
            }
            return null;
        }
        private static void WriteDateProperty(object obj, string propName, DateTime? dt)
        {
            var p = obj.GetType().GetProperty(propName);
            if (p == null) return;
            var t = p.PropertyType;

            if (t == typeof(DateTime)) { p.SetValue(obj, dt ?? default); return; }
            if (t == typeof(DateTime?)) { p.SetValue(obj, dt); return; }
            if (t.Name.Contains("DateOnly"))
            {
                if (dt == null) { p.SetValue(obj, null); return; }
                var ctor = t.GetConstructor(new[] { typeof(int), typeof(int), typeof(int) });
                var inst = ctor!.Invoke(new object[] { dt.Value.Year, dt.Value.Month, dt.Value.Day });
                p.SetValue(obj, inst);
            }
        }

        private void Notify(string message)
        {
            // View có thể show MessageBox trong code-behind nếu muốn;
            // hoặc bạn dùng event aggregator. Ở đây đơn giản: expose một string nếu cần.
            // Tối giản: dùng MessageBox trực tiếp là vi phạm MVVM; nên bạn có thể raise event.
            // Để gọn, mình không hiện tại đây. (Bạn có thể nối vào Snackbar/StatusText từ ViewModel.)
            System.Diagnostics.Debug.WriteLine(message);
        }
    }
}
