using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ManagementEmployee.Models;
using Microsoft.EntityFrameworkCore;

namespace ManagementEmployee.Services
{
    public class BackupService
    {
        private readonly ManagementEmployeeContext _context;
        private readonly ActivityLogService _activityLogService;

        public BackupService(ManagementEmployeeContext context, ActivityLogService activityLogService)
        {
            _context = context;
            _activityLogService = activityLogService;
        }

        /// <summary>
        /// Tạo file sao lưu JSON cho Employees (kèm DepartmentName).
        /// Trả về đường dẫn file và số lượng nhân viên đã xuất.
        /// </summary>
        public async Task<BackupResult> BackupEmployeesAsync(string? targetDirectory)
        {
            // Fallback thư mục Tài liệu 
            if (string.IsNullOrWhiteSpace(targetDirectory))
                targetDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            targetDirectory = Path.GetFullPath(targetDirectory);
            Directory.CreateDirectory(targetDirectory);

            // Lấy dữ liệu nhân viên (read-only) + phòng tránh null Department
            var employees = await _context.Employees
                .AsNoTracking()
                .Include(e => e.Department)
                .OrderBy(e => e.FullName)
                .Select(e => new EmployeeBackupRecord
                {
                    FullName = e.FullName,
                    DateOfBirth = e.DateOfBirth.ToDateTime(TimeOnly.MinValue),
                    Gender = e.Gender,
                    Address = e.Address,
                    Phone = e.Phone,
                    DepartmentName = (e.Department != null ? e.Department.DepartmentName : "Unknown") ?? "Unknown",
                    Position = e.Position ?? string.Empty,
                    BaseSalary = e.BaseSalary,
                    HireDate = e.HireDate.ToDateTime(TimeOnly.MinValue),
                    IsActive = e.IsActive,
                    CreatedAt = e.CreatedAt
                })
                .ToListAsync();

            var package = new EmployeeBackupPackage
            {
                SchemaVersion = 1,
                GeneratedAtUtc = DateTime.UtcNow,
                GeneratedBy = AppSession.CurrentUserEmail ?? AppSession.CurrentUserName ?? "System",
                EmployeeCount = employees.Count,
                Employees = employees
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true 
            };

            // Tên file đồng bộ với UI 
            var fileName = $"employees_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(targetDirectory, fileName);


            var tempPath = filePath + ".tmp";
            await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await JsonSerializer.SerializeAsync(stream, package, options);
            }
            if (File.Exists(filePath)) File.Delete(filePath);
            File.Move(tempPath, filePath);

            await _activityLogService.LogAsync(
                action: "Backup",
                entityName: nameof(Employee),
                details: $"Sao lưu {employees.Count} nhân viên vào {fileName}");

            return new BackupResult(filePath, employees.Count);
        }

        /// <summary>
        /// Phục hồi dữ liệu từ file JSON. Hỗ trợ ghi đè theo tùy chọn.
        /// </summary>
        public async Task<RestoreResult> RestoreEmployeesAsync(string filePath, bool overwriteExisting)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Đường dẫn tệp sao lưu không hợp lệ.", nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Không tìm thấy tệp sao lưu.", filePath);

            var readOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            var package = await JsonSerializer.DeserializeAsync<EmployeeBackupPackage>(stream, readOptions);
            if (package?.Employees == null)
                throw new InvalidOperationException("Tệp sao lưu không hợp lệ hoặc bị hỏng.");

            // Bản đồ Department theo tên 
            var departmentLookup = await _context.Departments
                .ToDictionaryAsync(d => d.DepartmentName, StringComparer.OrdinalIgnoreCase);

            // Bản đồ Employee theo key 
            var employeeLookup = await _context.Employees
                .Include(e => e.Department)
                .ToDictionaryAsync(e => BuildEmployeeKey(e.FullName, e.DateOfBirth), StringComparer.OrdinalIgnoreCase);

            var created = 0;
            var updated = 0;
            var skipped = 0;

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var record in package.Employees)
                {
                    if (string.IsNullOrWhiteSpace(record.FullName) ||
                        string.IsNullOrWhiteSpace(record.DepartmentName))
                    {
                        skipped++;
                        continue;
                    }

                    // Đảm bảo Department tồn tại
                    var departmentName = record.DepartmentName.Trim();
                    if (!departmentLookup.TryGetValue(departmentName, out var department))
                    {
                        department = new Department { DepartmentName = departmentName };
                        await _context.Departments.AddAsync(department);
                        await _context.SaveChangesAsync(); // cần ID để set FK
                        departmentLookup[departmentName] = department;
                    }

                    if (!record.DateOfBirth.HasValue)
                    {
                        skipped++;
                        continue;
                    }

                    var dob = DateOnly.FromDateTime(record.DateOfBirth.Value);
                    var position = string.IsNullOrWhiteSpace(record.Position) ? "Unknown" : record.Position.Trim();
                    var key = BuildEmployeeKey(record.FullName, dob);

                    if (employeeLookup.TryGetValue(key, out var existing))
                    {
                        if (overwriteExisting)
                        {
                            existing.DepartmentId = department.DepartmentId;
                            existing.Position = position;
                            existing.BaseSalary = record.BaseSalary;
                            existing.IsActive = record.IsActive;
                            existing.Address = record.Address;
                            existing.Phone = record.Phone;
                            existing.Gender = record.Gender;
                            existing.DateOfBirth = dob;
                            if (record.HireDate.HasValue)
                            {
                                existing.HireDate = DateOnly.FromDateTime(record.HireDate.Value);
                            }
                            updated++;
                        }
                        else
                        {
                            skipped++;
                        }
                        continue;
                    }

                    var newEmployee = new Employee
                    {
                        FullName = record.FullName.Trim(),
                        DepartmentId = department.DepartmentId,
                        Position = position,
                        BaseSalary = record.BaseSalary,
                        IsActive = record.IsActive,
                        Address = record.Address,
                        Phone = record.Phone,
                        Gender = record.Gender,
                        DateOfBirth = dob,
                        HireDate = record.HireDate.HasValue
                            ? DateOnly.FromDateTime(record.HireDate.Value)
                            : DateOnly.FromDateTime(DateTime.Today),
                        CreatedAt = record.CreatedAt == default ? DateTime.UtcNow : record.CreatedAt
                    };

                    await _context.Employees.AddAsync(newEmployee);
                    employeeLookup[key] = newEmployee;
                    created++;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _activityLogService.LogAsync(
                    action: "Restore",
                    entityName: nameof(Employee),
                    details: $"Phục hồi từ {Path.GetFileName(filePath)}. Tạo mới: {created}, cập nhật: {updated}, bỏ qua: {skipped}");

                return new RestoreResult(created, updated, skipped);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static string BuildEmployeeKey(string? fullName, DateOnly dateOfBirth)
        {
            var namePart = fullName?.Trim().ToLowerInvariant() ?? string.Empty;
            return $"{namePart}|{dateOfBirth:yyyyMMdd}";
        }
    }

    public sealed record BackupResult(string FilePath, int EmployeeCount);
    public sealed record RestoreResult(int Created, int Updated, int Skipped);

    public sealed class EmployeeBackupPackage
    {
        public int SchemaVersion { get; set; }
        public DateTime GeneratedAtUtc { get; set; }
        public string? GeneratedBy { get; set; }
        public int EmployeeCount { get; set; }
        public List<EmployeeBackupRecord>? Employees { get; set; }
    }

    public sealed class EmployeeBackupRecord
    {
        public string FullName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public decimal BaseSalary { get; set; }
        public DateTime? HireDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
