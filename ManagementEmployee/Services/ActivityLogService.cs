using ManagementEmployee.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagementEmployee.Services
{
    public class ActivityLogService
    {
        
        private ManagementEmployeeContext NewDb() => new ManagementEmployeeContext();


        public async Task LogAsync(string action, string entityName, long? entityId = null, string? details = null, int? userId = null)
        {
            using var db = NewDb();
            var log = new ActivityLog
            {
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Details = details,
                UserId = userId ?? AppSession.CurrentUserId, // nếu không dùng AppSession, thay bằng userId truyền vào
                CreatedAt = DateTime.UtcNow
            };

            db.ActivityLogs.Add(log);
            await db.SaveChangesAsync();
        }

        // danh sách user active
        public async Task<List<UserLookupItem>> GetActiveUsersAsync()
        {
            using var db = NewDb();
            return await db.Users
                .Include(u => u.Employee)
                .Where(u => u.IsActive)
                .OrderBy(u => u.Employee != null ? u.Employee.FullName : u.Email)
                .Select(u => new UserLookupItem
                {
                    UserId = u.UserId,
                    DisplayName = u.Employee != null && !string.IsNullOrEmpty(u.Employee.FullName)
                                  ? u.Employee.FullName
                                  : u.Email
                })
                .ToListAsync();
        }

        // Lấy logs theo filter 
        public async Task<List<ActivityLogDto>> GetLogsAsync(
            DateTime? from = null, DateTime? to = null, int? userId = null, string? keyword = null,
            int pageNumber = 1, int pageSize = 100)
        {
            using var db = NewDb();

            var q = db.ActivityLogs
                .Include(l => l.User).ThenInclude(u => u.Employee)
                .AsNoTracking()
                .AsQueryable();

            if (from.HasValue) q = q.Where(log => log.CreatedAt >= from.Value);
            if (to.HasValue) q = q.Where(log => log.CreatedAt <= to.Value);
            if (userId.HasValue && userId.Value > 0) q = q.Where(log => log.UserId == userId.Value);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                q = q.Where(log =>
                    (log.Action != null && EF.Functions.Like(log.Action, $"%{kw}%")) ||
                    (log.EntityName != null && EF.Functions.Like(log.EntityName, $"%{kw}%")) ||
                    (log.Details != null && EF.Functions.Like(log.Details, $"%{kw}%")) ||
                    (log.EntityId != null && EF.Functions.Like(log.EntityId.ToString(), $"%{kw}%")) ||
                    (log.User != null &&
                        EF.Functions.Like(
                            (log.User.Employee != null ? log.User.Employee.FullName : log.User.Email),
                            $"%{kw}%"
                        )
                    )
                );
            }

            var list = await q
                .OrderByDescending(log => log.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(log => new ActivityLogDto
                {
                    // map từ ActivityId (long) của entity sang LogId (DTO của project)
                    LogId = (int)log.ActivityId,
                    CreatedAt = log.CreatedAt,
                    UserId = log.UserId,
                    UserDisplayName = log.User != null
                        ? (log.User.Employee != null && !string.IsNullOrEmpty(log.User.Employee.FullName)
                            ? log.User.Employee.FullName
                            : log.User.Email)
                        : "Hệ thống",
                    Action = log.Action ?? string.Empty,
                    EntityName = log.EntityName ?? string.Empty,
                    // DTO dùng string cho EntityId 
                    EntityId = log.EntityId.HasValue ? log.EntityId.Value.ToString() : string.Empty,
                    Details = log.Details ?? string.Empty
                })
                .ToListAsync();

            return list;
        }

        // Export CSV
        public async Task<string> ExportToExcelAsync(IEnumerable<ActivityLogDto> logs)
        {
            return await Task.Run(() =>
            {
                var data = logs?.ToList() ?? new List<ActivityLogDto>();
                if (data.Count == 0) throw new InvalidOperationException("Không có bản ghi để xuất.");

                var filePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    $"NhatKyHoatDong_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                );

                using var sw = new StreamWriter(filePath, false, Encoding.UTF8);
                // Header
                sw.WriteLine("Id,ThoiGian,NguoiThucHien,HanhDong,ThucThe,MaThucThe,ChiTiet");

                foreach (var log in data.OrderByDescending(l => l.CreatedAt))
                {
                    var row = new[]
                    {
                        log.LogId.ToString(),
                        log.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss"),
                        Csv(log.UserDisplayName),
                        Csv(log.Action),
                        Csv(log.EntityName),
                        Csv(log.EntityId ?? string.Empty),
                        Csv(log.Details ?? string.Empty)
                    };
                    sw.WriteLine(string.Join(",", row));
                }

                return filePath;
            });
        }

        private static string Csv(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return (s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
                ? $"\"{s.Replace("\"", "\"\"")}\""
                : s;
        }
    }
}
