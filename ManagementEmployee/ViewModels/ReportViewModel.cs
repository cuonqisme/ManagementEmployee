using ManagementEmployee.Models;
using ManagementEmployee.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ManagementEmployee.ViewModels
{
    public class ReportViewModel : BaseViewModel
    {
        private readonly StatisticService _statisticService;
        private readonly ReportService _reportService;

        private int _selectedYear = DateTime.Now.Year;
        private int _selectedQuarter = 1;
        private int _activeEmployeeCount;
        private int _inactiveEmployeeCount;
        private decimal _totalAnnualGross;
        private decimal _totalAnnualNet;
        private QuarterlySalaryStatistic? _selectedQuarterStatistic;

        // sự kiện để Page hiển thị MessageBox
        public event EventHandler<string>? MessageShown;
        public event EventHandler<string>? ErrorShown;
        protected void ShowMessage(string m) => MessageShown?.Invoke(this, m);
        protected void ShowError(string m) => ErrorShown?.Invoke(this, m);

        public ObservableCollection<DepartmentStatistic> DepartmentStatistics { get; } = new();
        public ObservableCollection<PositionStatistic> PositionStatistics { get; } = new();
        public ObservableCollection<GenderStatistic> GenderStatistics { get; } = new();
        public ObservableCollection<MonthlySalaryStatistic> MonthlySalaryStatistics { get; } = new();
        public ObservableCollection<QuarterlySalaryStatistic> QuarterlySalaryStatistics { get; } = new();
        public ObservableCollection<int> AvailableYears { get; } = new();

        public ICommand ExportEmployeeByDepartmentCommand { get; }
        public ICommand ExportEmployeeByPositionCommand { get; }
        public ICommand ExportSalaryByMonthCommand { get; }
        public ICommand ExportSalaryByQuarterCommand { get; }
        public ICommand ExportSalaryByMonthPdfCommand { get; }
        public ICommand ExportSalaryByQuarterPdfCommand { get; }
        public ICommand LoadStatisticsCommand { get; }

        public ReportViewModel(StatisticService statisticService, ReportService reportService)
        {
            _statisticService = statisticService;
            _reportService = reportService;

            ExportEmployeeByDepartmentCommand = new AsyncRelayCommand(async _ => await ExportEmployeeByDepartmentAsync());
            ExportEmployeeByPositionCommand = new AsyncRelayCommand(async _ => await ExportEmployeeByPositionAsync());
            ExportSalaryByMonthCommand = new AsyncRelayCommand(async _ => await ExportSalaryByMonthAsync());
            ExportSalaryByQuarterCommand = new AsyncRelayCommand(async _ => await ExportSalaryByQuarterAsync());
            ExportSalaryByMonthPdfCommand = new AsyncRelayCommand(async _ => await ExportSalaryByMonthPdfAsync());
            ExportSalaryByQuarterPdfCommand = new AsyncRelayCommand(async _ => await ExportSalaryByQuarterPdfAsync());
            LoadStatisticsCommand = new AsyncRelayCommand(async _ => await LoadStatisticsAsync());
        }

        public IReadOnlyList<int> QuarterOptions { get; } = new[] { 1, 2, 3, 4 };

        public int SelectedYear
        {
            get => _selectedYear;
            set { if (SetProperty(ref _selectedYear, value)) _ = LoadSalaryStatisticsAsync(); }
        }

        public int SelectedQuarter
        {
            get => _selectedQuarter;
            set { if (SetProperty(ref _selectedQuarter, value)) UpdateSelectedQuarterStatistic(); }
        }

        public int ActiveEmployeeCount
        {
            get => _activeEmployeeCount;
            private set => SetProperty(ref _activeEmployeeCount, value);
        }

        public int InactiveEmployeeCount
        {
            get => _inactiveEmployeeCount;
            private set => SetProperty(ref _inactiveEmployeeCount, value);
        }

        public decimal TotalAnnualGross
        {
            get => _totalAnnualGross;
            private set => SetProperty(ref _totalAnnualGross, value);
        }

        public decimal TotalAnnualNet
        {
            get => _totalAnnualNet;
            private set => SetProperty(ref _totalAnnualNet, value);
        }

        public bool HasMonthlyData => MonthlySalaryStatistics.Count > 0;
        public bool HasQuarterlyData => QuarterlySalaryStatistics.Count > 0;

        public QuarterlySalaryStatistic? SelectedQuarterStatistic
        {
            get => _selectedQuarterStatistic;
            private set => SetProperty(ref _selectedQuarterStatistic, value);
        }

        public async Task LoadStatisticsAsync()
        {
            try
            {
                IsLoading = true;

                // nhân viên theo phòng ban
                var departments = await _statisticService.GetEmployeeByDepartmentAsync();
                DepartmentStatistics.Clear();
                foreach (var d in departments) DepartmentStatistics.Add(d);

                ActiveEmployeeCount = departments.Sum(d => d.TotalEmployees);
                InactiveEmployeeCount = departments.Sum(d => d.InactiveEmployees);

                // theo chức vụ
                var positions = await _statisticService.GetEmployeeByPositionAsync();
                PositionStatistics.Clear();
                foreach (var p in positions) PositionStatistics.Add(p);

                // theo giới
                var genders = await _statisticService.GetEmployeeByGenderAsync();
                var totalEmp = Math.Max(1, genders.Sum(g => g.Count));
                GenderStatistics.Clear();
                foreach (var g in genders)
                {
                    g.Percentage = g.Count * 100.0 / totalEmp;
                    GenderStatistics.Add(g);
                }

                // năm có dữ liệu payroll
                var years = await _statisticService.GetAvailableYearsAsync();
                AvailableYears.Clear();
                foreach (var y in years.OrderByDescending(x => x)) AvailableYears.Add(y);
                if (AvailableYears.Count == 0) AvailableYears.Add(DateTime.Now.Year);
                if (!AvailableYears.Contains(SelectedYear)) SelectedYear = AvailableYears.First();

                await LoadSalaryStatisticsAsync();
            }
            finally { IsLoading = false; }
        }

        private async Task LoadSalaryStatisticsAsync()
        {
            var monthly = await _statisticService.GetSalaryByMonthAsync(SelectedYear);
            MonthlySalaryStatistics.Clear();
            foreach (var m in monthly) MonthlySalaryStatistics.Add(m);

            var quarterly = await _statisticService.GetSalaryByQuarterAsync(SelectedYear);
            QuarterlySalaryStatistics.Clear();
            foreach (var q in quarterly) QuarterlySalaryStatistics.Add(q);

            TotalAnnualGross = MonthlySalaryStatistics.Sum(m => m.TotalGross);
            TotalAnnualNet = MonthlySalaryStatistics.Sum(m => m.TotalNet);

            if (QuarterlySalaryStatistics.Count > 0 && QuarterlySalaryStatistics.All(q => q.Quarter != SelectedQuarter))
                SelectedQuarter = QuarterlySalaryStatistics.First().Quarter;
            else
                UpdateSelectedQuarterStatistic();

            OnPropertyChanged(nameof(HasMonthlyData));
            OnPropertyChanged(nameof(HasQuarterlyData));
        }

        private void UpdateSelectedQuarterStatistic()
            => SelectedQuarterStatistic = QuarterlySalaryStatistics.FirstOrDefault(q => q.Quarter == SelectedQuarter);

        // Exports
        private async Task ExportEmployeeByDepartmentAsync()
        {
            try { var path = await _reportService.ExportEmployeeByDepartmentExcelAsync(); ShowMessage("Xuất file thành công!"); Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
            catch (Exception ex) { ShowError(ex.Message); }
        }
        private async Task ExportEmployeeByPositionAsync()
        {
            try { var path = await _reportService.ExportEmployeeByPositionExcelAsync(); ShowMessage("Xuất file thành công!"); Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
            catch (Exception ex) { ShowError(ex.Message); }
        }
        private async Task ExportSalaryByMonthAsync()
        {
            try { var path = await _reportService.ExportSalaryByMonthExcelAsync(SelectedYear); ShowMessage("Xuất file Excel thành công!"); Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
            catch (Exception ex) { ShowError(ex.Message); }
        }
        private async Task ExportSalaryByQuarterAsync()
        {
            try { var path = await _reportService.ExportSalaryByQuarterExcelAsync(SelectedYear); ShowMessage("Xuất file Excel thành công!"); Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
            catch (Exception ex) { ShowError(ex.Message); }
        }
        private async Task ExportSalaryByMonthPdfAsync()
        {
            try { var path = await _reportService.ExportSalaryByMonthPdfAsync(SelectedYear); ShowMessage("Xuất file PDF thành công!"); Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
            catch (Exception ex) { ShowError(ex.Message); }
        }
        private async Task ExportSalaryByQuarterPdfAsync()
        {
            try { var path = await _reportService.ExportSalaryByQuarterPdfAsync(SelectedYear); ShowMessage("Xuất file PDF thành công!"); Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
            catch (Exception ex) { ShowError(ex.Message); }
        }
    }
}
