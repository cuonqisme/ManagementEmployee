using ManagementEmployee.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ManagementEmployee.ViewModels
{
    public class AttendanceManagerViewModel : INotifyPropertyChanged
    {
        // ====== Observable state ======
        public ObservableCollection<Employee> Employees { get; } = new();
        private Employee _selectedEmployee;
        public Employee SelectedEmployee { get => _selectedEmployee; set { _selectedEmployee = value; OnPropertyChanged(); } }

        private DateTime? _selectedWorkDate = DateTime.Today;
        public DateTime? SelectedWorkDate { get => _selectedWorkDate; set { _selectedWorkDate = value; OnPropertyChanged(); RecalcWorkHours(); } }

        private string _checkInText;
        public string CheckInText { get => _checkInText; set { _checkInText = value; OnPropertyChanged(); RecalcWorkHours(); } }

        private string _checkOutText;
        public string CheckOutText { get => _checkOutText; set { _checkOutText = value; OnPropertyChanged(); RecalcWorkHours(); } }

        private string _workHoursText;
        public string WorkHoursText { get => _workHoursText; set { _workHoursText = value; OnPropertyChanged(); } }

        private string _overtimeText = "0";
        public string OvertimeText { get => _overtimeText; set { _overtimeText = value; OnPropertyChanged(); } }

        public ObservableCollection<string> StatusOptions { get; } =
            new(new[] { "Present", "Leave", "WFH", "Absent" });

        private string _selectedStatus = "Present";
        public string SelectedStatus { get => _selectedStatus; set { _selectedStatus = value; OnPropertyChanged(); } }

        private string _notes;
        public string Notes { get => _notes; set { _notes = value; OnPropertyChanged(); } }

        // Report
        public ObservableCollection<int> ReportMonthOptions { get; } =
            new(Enumerable.Range(1, 12));

        public ObservableCollection<int> ReportYearOptions { get; } =
            new(Enumerable.Range(DateTime.Today.Year - 3, 5));

        private int _selectedReportMonth = DateTime.Today.Month;
        public int SelectedReportMonth { get => _selectedReportMonth; set { _selectedReportMonth = value; OnPropertyChanged(); } }

        private int _selectedReportYear = DateTime.Today.Year;
        public int SelectedReportYear { get => _selectedReportYear; set { _selectedReportYear = value; OnPropertyChanged(); } }

        public ObservableCollection<AttendanceReportRow> ReportItems { get; } = new();

        // ====== Commands ======
        public ICommand NewCommand { get; }
        public ICommand LoadCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand GenerateReportCommand { get; }

        public AttendanceManagerViewModel()
        {
            NewCommand = new RelayCommand(_ => NewForm());
            LoadCommand = new AsyncRelayCommand(LoadAsync, CanLoadOrSave);
            SaveCommand = new AsyncRelayCommand(SaveAsync, CanLoadOrSave);
            GenerateReportCommand = new AsyncRelayCommand(GenerateReportAsync);
        }

        // ====== Init ======
        public async Task InitAsync()
        {
            try
            {
                using var db = NewDb();
                var emps = await db.Employees.AsNoTracking()
                                  .Where(e => e.IsActive)
                                  .OrderBy(e => e.FullName)
                                  .ToListAsync();

                Employees.Clear();
                foreach (var e in emps) Employees.Add(e);
                SelectedEmployee = Employees.FirstOrDefault();

                SelectedWorkDate = DateTime.Today;
            }
            catch
            {
                // swallow for UI
            }
        }

        // ====== Actions ======
        private bool CanLoadOrSave(object _)
            => SelectedEmployee != null && SelectedWorkDate.HasValue;

        private async Task LoadAsync(object _)
        {
            if (!CanLoadOrSave(null)) return;

            var d = ToDateOnly(SelectedWorkDate.Value.Date);

            try
            {
                using var db = NewDb();
                var att = await db.Attendances.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.EmployeeId == SelectedEmployee.EmployeeId && x.WorkDate == d);

                if (att == null)
                {
                    CheckInText = "";
                    CheckOutText = "";
                    WorkHoursText = "";
                    OvertimeText = "0";
                    SelectedStatus = "Present";
                    Notes = "";
                    return;
                }

                CheckInText = att.CheckIn.HasValue ? att.CheckIn.Value.ToString("HH:mm") : "";
                CheckOutText = att.CheckOut.HasValue ? att.CheckOut.Value.ToString("HH:mm") : "";
                WorkHoursText = att.WorkHours.ToString("N2");
                OvertimeText = att.OvertimeHours.ToString("N2");
                SelectedStatus = string.IsNullOrWhiteSpace(att.Status) ? "Present" : att.Status;
                Notes = att.Notes ?? "";
            }
            catch
            {
                // swallow for UI
            }
        }

        private async Task SaveAsync(object _)
        {
            if (!CanLoadOrSave(null)) return;

            var workDate = ToDateOnly(SelectedWorkDate.Value.Date);

            DateTime? checkIn = null, checkOut = null;
            if (TryParseTime(CheckInText, out var tin)) checkIn = ComposeDateTime(workDate, tin);
            if (TryParseTime(CheckOutText, out var tout)) checkOut = ComposeDateTime(workDate, tout);

            decimal hours = 0m;
            if (checkIn.HasValue && checkOut.HasValue && checkOut > checkIn)
                hours = Math.Round((decimal)(checkOut.Value - checkIn.Value).TotalHours, 2);

            decimal ot = Math.Round(ParseDec(OvertimeText), 2);
            if (ot < 0) ot = 0;

            try
            {
                using var db = NewDb();
                var existing = await db.Attendances
                    .FirstOrDefaultAsync(x => x.EmployeeId == SelectedEmployee.EmployeeId && x.WorkDate == workDate);

                if (existing == null)
                {
                    var att = new Attendance
                    {
                        EmployeeId = SelectedEmployee.EmployeeId,
                        WorkDate = workDate,
                        CheckIn = checkIn,
                        CheckOut = checkOut,
                        WorkHours = hours,
                        OvertimeHours = ot,
                        Status = SelectedStatus ?? "Present",
                        Notes = Notes?.Trim()
                    };
                    db.Attendances.Add(att);
                }
                else
                {
                    existing.CheckIn = checkIn;
                    existing.CheckOut = checkOut;
                    existing.WorkHours = hours;
                    existing.OvertimeHours = ot;
                    existing.Status = SelectedStatus ?? "Present";
                    existing.Notes = Notes?.Trim();
                }

                await db.SaveChangesAsync();
                WorkHoursText = hours.ToString("N2", CultureInfo.CurrentCulture);
            }
            catch
            {
                // swallow for UI
            }
        }

        private async Task GenerateReportAsync(object _)
        {
            var mm = SelectedReportMonth;
            var yy = SelectedReportYear;
            var start = new DateOnly(yy, mm, 1);
            var end = start.AddMonths(1).AddDays(-1);

            try
            {
                using var db = NewDb();

                var query =
                    from emp in db.Employees.AsNoTracking()
                    where emp.IsActive
                    join att in db.Attendances.AsNoTracking()
                        .Where(x => x.WorkDate >= start && x.WorkDate <= end)
                        on emp.EmployeeId equals att.EmployeeId into gj
                    select new AttendanceReportRow
                    {
                        FullName = emp.FullName,
                        Present = gj.Count(x => x.Status == "Present"),
                        Leave = gj.Count(x => x.Status == "Leave"),
                        WFH = gj.Count(x => x.Status == "WFH"),
                        Absent = gj.Count(x => x.Status == "Absent"),
                        WorkHours = gj.Sum(x => (decimal?)x.WorkHours) ?? 0m,
                        OvertimeHours = gj.Sum(x => (decimal?)x.OvertimeHours) ?? 0m
                    };

                var result = await query.OrderBy(x => x.FullName).ToListAsync();

                ReportItems.Clear();
                foreach (var r in result) ReportItems.Add(r);
            }
            catch
            {
                // swallow for UI
            }
        }

        private void NewForm()
        {
            CheckInText = "";
            CheckOutText = "";
            WorkHoursText = "";
            OvertimeText = "0";
            SelectedStatus = "Present";
            Notes = "";
            if (!SelectedWorkDate.HasValue) SelectedWorkDate = DateTime.Today;
        }

        // ====== Helpers ======
        private static ManagementEmployeeContext NewDb() => new();

        private static DateOnly ToDateOnly(DateTime dt) => DateOnly.FromDateTime(dt);

        private static DateTime ComposeDateTime(DateOnly d, TimeSpan t)
            => new DateTime(d.Year, d.Month, d.Day, t.Hours, t.Minutes, t.Seconds);

        private void RecalcWorkHours()
        {
            WorkHoursText = "";
            if (!SelectedWorkDate.HasValue) return;

            var d = ToDateOnly(SelectedWorkDate.Value.Date);

            DateTime? checkIn = null, checkOut = null;
            if (TryParseTime(CheckInText, out var tin)) checkIn = ComposeDateTime(d, tin);
            if (TryParseTime(CheckOutText, out var tout)) checkOut = ComposeDateTime(d, tout);

            if (checkIn.HasValue && checkOut.HasValue && checkOut > checkIn)
            {
                var hours = (decimal)(checkOut.Value - checkIn.Value).TotalHours;
                WorkHoursText = Math.Round(hours, 2).ToString("N2", CultureInfo.CurrentCulture);
            }
        }

        private static bool TryParseTime(string text, out TimeSpan ts)
        {
            ts = default;
            text = (text ?? "").Trim();
            if (string.IsNullOrEmpty(text)) return false;

            return TimeSpan.TryParseExact(
                text,
                new[] { "h\\:mm", "hh\\:mm", "H\\:mm", "HH\\:mm" },
                CultureInfo.InvariantCulture,
                out ts);
        }

        private static decimal ParseDec(string s)
        {
            s = (s ?? "").Trim();
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out var v)) return v;
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out v)) return v;
            var compact = s.Replace(".", "").Replace(",", "");
            return decimal.TryParse(compact, NumberStyles.Number, CultureInfo.InvariantCulture, out v) ? v : 0m;
        }

        // INPC
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class AttendanceReportRow
    {
        public string FullName { get; set; }
        public int Present { get; set; }
        public int Leave { get; set; }
        public int WFH { get; set; }
        public int Absent { get; set; }
        public decimal WorkHours { get; set; }
        public decimal OvertimeHours { get; set; }
    }
}
