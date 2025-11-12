using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ManagementEmployee.Models;
using Microsoft.EntityFrameworkCore;
using ManagementEmployee.ViewModels;

namespace ManagementEmployee.ViewModels
{
    public sealed class DepartmentManagerViewModel : ViewModelBase
    {
        public ObservableCollection<Department> Departments { get; } = new();
        public ObservableCollection<EmpRow> DepartmentEmployees { get; } = new();
        public ObservableCollection<EmpItem> AvailableEmployees { get; } = new();

        private Department _selectedDepartment;
        public Department SelectedDepartment
        {
            get => _selectedDepartment;
            set
            {
                if (SetProperty(ref _selectedDepartment, value))
                {
                    DepartmentName = value?.DepartmentName ?? string.Empty;
                    _ = LoadEmployeesForDeptAsync(value?.DepartmentId);
                }
            }
        }

        private string _departmentName = "";
        public string DepartmentName
        {
            get => _departmentName;
            set => SetProperty(ref _departmentName, value);
        }

        private string _deptSummary = "No department selected.";
        public string DeptSummary
        {
            get => _deptSummary;
            set => SetProperty(ref _deptSummary, value);
        }

        private string _empCountText = "";
        public string EmpCountText
        {
            get => _empCountText;
            set => SetProperty(ref _empCountText, value);
        }

        // Commands
        public AsyncRelayCommand LoadedCommand { get; }
        public AsyncRelayCommand AddCommand { get; }
        public AsyncRelayCommand SaveCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }
        public AsyncRelayCommand RefreshCommand { get; }

        public DepartmentManagerViewModel()
        {
            LoadedCommand = new AsyncRelayCommand(_ => InitializeAsync());
            AddCommand = new AsyncRelayCommand(_ => AddDepartmentAsync());
            SaveCommand = new AsyncRelayCommand(_ => SaveDepartmentAsync());
            DeleteCommand = new AsyncRelayCommand(_ => DeleteDepartmentAsync());
            RefreshCommand = new AsyncRelayCommand(_ => RefreshAsync());
        }

        private ManagementEmployeeContext NewDb() => new ManagementEmployeeContext();

        // ===== Init =====
        private async Task InitializeAsync()
        {
            await LoadDepartmentsAsync();
            if (SelectedDepartment != null)
                await LoadEmployeesForDeptAsync(SelectedDepartment.DepartmentId);
            else
                UpdateSummary(null, 0);
        }

        // ===== Loads =====
        private async Task LoadDepartmentsAsync()
        {
            try
            {
                using var db = NewDb();
                var list = await db.Departments.AsNoTracking()
                    .OrderBy(d => d.DepartmentName)
                    .ToListAsync();

                Departments.Clear();
                foreach (var d in list) Departments.Add(d);

                if (SelectedDepartment == null && Departments.Count > 0)
                    SelectedDepartment = Departments.First();
                else if (SelectedDepartment != null)
                {
                    var pick = Departments.FirstOrDefault(x => x.DepartmentId == SelectedDepartment.DepartmentId);
                    SelectedDepartment = pick; // sẽ kích hoạt load employees
                }
            }
            catch (Exception ex)
            {
                DeptSummary = "Load departments failed: " + ex.Message;
            }
        }

        private async Task LoadEmployeesForDeptAsync(int? deptId)
        {
            DepartmentEmployees.Clear();
            AvailableEmployees.Clear();

            if (!deptId.HasValue)
            {
                UpdateSummary(null, 0);
                return;
            }

            try
            {
                using var db = NewDb();

                var inDept = await db.Employees.AsNoTracking()
                    .Where(e => e.DepartmentId == deptId.Value && e.IsActive)
                    .OrderBy(e => e.FullName)
                    .Select(e => new EmpRow
                    {
                        EmployeeId = e.EmployeeId,
                        FullName = e.FullName,
                        Position = e.Position,
                        Phone = e.Phone
                    })
                    .ToListAsync();

                foreach (var r in inDept) DepartmentEmployees.Add(r);
                EmpCountText = $"Employees in dept: {DepartmentEmployees.Count}";

                var available = await db.Employees.AsNoTracking()
                    .Where(e => e.IsActive && e.DepartmentId != deptId.Value)
                    .OrderBy(e => e.FullName)
                    .Select(e => new EmpItem { Id = e.EmployeeId, Name = e.FullName })
                    .ToListAsync();

                foreach (var a in available) AvailableEmployees.Add(a);

                UpdateSummary(SelectedDepartment, DepartmentEmployees.Count);
            }
            catch (Exception ex)
            {
                DeptSummary = "Load employees failed: " + ex.Message;
            }
        }

        private void UpdateSummary(Department dept, int empCount)
        {
            if (dept == null)
            {
                DeptSummary = "No department selected.";
                return;
            }
            DeptSummary = $"Selected: {dept.DepartmentName}  •  Created: {dept.CreatedAt:yyyy-MM-dd HH:mm}  •  Employees: {empCount}";
        }

        // ===== CRUD Dept =====
        private async Task AddDepartmentAsync()
        {
            string name = (DepartmentName ?? "").Trim();
            if (name.Length < 2) { DeptSummary = "Enter department name (≥ 2 chars)."; return; }

            try
            {
                using var db = NewDb();
                bool existed = await db.Departments.AnyAsync(x => x.DepartmentName == name);
                if (existed) { DeptSummary = "Department name already exists."; return; }

                var dep = new Department { DepartmentName = name, CreatedAt = DateTime.UtcNow };
                db.Departments.Add(dep);
                await db.SaveChangesAsync();

                await LoadDepartmentsAsync();
                SelectedDepartment = Departments.First(d => d.DepartmentId == dep.DepartmentId);
                DeptSummary = "Department created.";
            }
            catch (Exception ex) { DeptSummary = "Add failed: " + ex.Message; }
        }

        private async Task SaveDepartmentAsync()
        {
            if (SelectedDepartment == null) { DeptSummary = "Select a department first."; return; }

            string name = (DepartmentName ?? "").Trim();
            if (name.Length < 2) { DeptSummary = "Enter department name (≥ 2 chars)."; return; }

            try
            {
                using var db = NewDb();
                var dep = await db.Departments.FirstOrDefaultAsync(x => x.DepartmentId == SelectedDepartment.DepartmentId);
                if (dep == null) { DeptSummary = "Department not found."; return; }

                bool dup = await db.Departments.AnyAsync(x => x.DepartmentName == name && x.DepartmentId != dep.DepartmentId);
                if (dup) { DeptSummary = "Another department with the same name exists."; return; }

                dep.DepartmentName = name;
                await db.SaveChangesAsync();

                await LoadDepartmentsAsync();
                DeptSummary = "Saved.";
            }
            catch (Exception ex) { DeptSummary = "Save failed: " + ex.Message; }
        }

        private async Task DeleteDepartmentAsync()
        {
            if (SelectedDepartment == null) { DeptSummary = "Select a department first."; return; }

            try
            {
                using var db = NewDb();
                var dep = await db.Departments.FirstOrDefaultAsync(x => x.DepartmentId == SelectedDepartment.DepartmentId);
                if (dep == null) { DeptSummary = "Department not found."; return; }

                bool hasEmp = await db.Employees.AnyAsync(x => x.DepartmentId == dep.DepartmentId);
                if (hasEmp) { DeptSummary = "Cannot delete. There are employees in this department."; return; }

                db.Departments.Remove(dep);
                await db.SaveChangesAsync();

                DepartmentName = "";
                await LoadDepartmentsAsync();
                DepartmentEmployees.Clear();
                AvailableEmployees.Clear();
                UpdateSummary(null, 0);

                DeptSummary = "Deleted.";
            }
            catch (Exception ex) { DeptSummary = "Delete failed: " + ex.Message; }
        }

        private async Task RefreshAsync()
        {
            await LoadDepartmentsAsync();
            if (SelectedDepartment != null)
                await LoadEmployeesForDeptAsync(SelectedDepartment.DepartmentId);
        }

        // ===== Assign/Remove (gọi từ View, truyền SelectedItems vào) =====
        public async Task AssignAsync(IList selectedEmpItems)
        {
            if (SelectedDepartment == null) { DeptSummary = "Select a department first."; return; }
            if (selectedEmpItems == null || selectedEmpItems.Count == 0) { DeptSummary = "Select employees to assign."; return; }

            try
            {
                using var db = NewDb();
                foreach (var it in selectedEmpItems)
                {
                    if (it is EmpItem row)
                    {
                        var emp = await db.Employees.FirstOrDefaultAsync(x => x.EmployeeId == row.Id);
                        if (emp != null) emp.DepartmentId = SelectedDepartment.DepartmentId;
                    }
                }
                await db.SaveChangesAsync();

                await LoadEmployeesForDeptAsync(SelectedDepartment.DepartmentId);
                DeptSummary = "Assigned.";
            }
            catch (Exception ex) { DeptSummary = "Assign failed: " + ex.Message; }
        }

        public async Task RemoveAsync(IList selectedRows)
        {
            if (SelectedDepartment == null) { DeptSummary = "Select a department first."; return; }
            if (selectedRows == null || selectedRows.Count == 0) { DeptSummary = "Select employees to remove."; return; }

            try
            {
                using var db = NewDb();
                var allDeptIds = await db.Departments.AsNoTracking()
                    .OrderBy(x => x.DepartmentId).Select(x => x.DepartmentId).ToListAsync();

                int fallback = allDeptIds.FirstOrDefault(x => x != SelectedDepartment.DepartmentId);
                if (fallback == 0) { DeptSummary = "No other department to move employees into."; return; }

                foreach (var it in selectedRows)
                {
                    if (it is EmpRow row)
                    {
                        var emp = await db.Employees.FirstOrDefaultAsync(x => x.EmployeeId == row.EmployeeId);
                        if (emp != null) emp.DepartmentId = fallback;
                    }
                }
                await db.SaveChangesAsync();

                await LoadEmployeesForDeptAsync(SelectedDepartment.DepartmentId);
                DeptSummary = "Removed from department.";
            }
            catch (Exception ex) { DeptSummary = "Remove failed: " + ex.Message; }
        }

        // ===== DTOs =====
        public sealed class EmpItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public override string ToString() => Name;
        }

        public sealed class EmpRow
        {
            public int EmployeeId { get; set; }
            public string FullName { get; set; }
            public string Position { get; set; }
            public string Phone { get; set; }
        }
    }
}
