using ManagementEmployee.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManagementEmployee.Services
{
    public class NotificationService
    {
        private readonly ManagementEmployeeContext _context;
        public event EventHandler<NotificationEventArgs>? NotificationReceived;

        public NotificationService(ManagementEmployeeContext context)
        {
            _context = context;
        }

        // ---- GỬI CHO TẤT CẢ NHÂN VIÊN ----
        public async Task<bool> SendNotificationToAllAsync(int senderUserId, string title, string content)
        {
            try
            {
                // Lấy tất cả user đang active và Employee đang active (nếu có)
                var receiverIds = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.IsActive &&
                                (u.EmployeeId == null || u.Employee!.IsActive))
                    .Select(u => u.UserId)
                    .ToListAsync();

                if (receiverIds.Count == 0)
                    return false;

                var notifications = new List<Notification>(receiverIds.Count);
                foreach (var rid in receiverIds)
                {
                    notifications.Add(new Notification
                    {
                        Title = title,
                        Content = content,
                        SenderUserId = senderUserId,
                        ReceiverUserId = rid,
                        DepartmentId = null,
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });
                }

                // Bảo đảm người gửi có 1 bản sao (nếu không nằm trong receiverIds)
                if (!receiverIds.Contains(senderUserId))
                {
                    notifications.Add(new Notification
                    {
                        Title = title,
                        Content = content,
                        SenderUserId = senderUserId,
                        ReceiverUserId = senderUserId,
                        DepartmentId = null,
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });
                }

                await _context.Notifications.AddRangeAsync(notifications);
                await _context.SaveChangesAsync();

                await LogNotificationAsync(senderUserId, title, "toàn bộ nhân viên", notifications.Count);
                OnNotificationReceived(new NotificationEventArgs { Title = title, Content = content });
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ---- GỬI THEO PHÒNG BAN ----
        public async Task<bool> SendNotificationToDepartmentAsync(int senderUserId, int departmentId, string title, string content)
        {
            try
            {
                // Tìm user thuộc nhân viên của phòng ban
                var receiverIds = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.IsActive
                                && u.EmployeeId != null
                                && u.Employee!.IsActive
                                && u.Employee!.DepartmentId == departmentId)
                    .Select(u => u.UserId)
                    .ToListAsync();

                if (receiverIds.Count == 0)
                    return false;

                var notifications = new List<Notification>(receiverIds.Count);
                foreach (var rid in receiverIds)
                {
                    notifications.Add(new Notification
                    {
                        Title = title,
                        Content = content,
                        SenderUserId = senderUserId,
                        ReceiverUserId = rid,
                        DepartmentId = departmentId,
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });
                }

                // Bảo đảm người gửi thấy bản sao
                if (!receiverIds.Contains(senderUserId))
                {
                    notifications.Add(new Notification
                    {
                        Title = title,
                        Content = content,
                        SenderUserId = senderUserId,
                        ReceiverUserId = senderUserId,
                        DepartmentId = departmentId,
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });
                }

                await _context.Notifications.AddRangeAsync(notifications);
                await _context.SaveChangesAsync();

                var departmentName = (await _context.Departments.FindAsync(departmentId))?.DepartmentName ?? "phòng ban";
                await LogNotificationAsync(senderUserId, title, $"phòng ban {departmentName}", notifications.Count);

                OnNotificationReceived(new NotificationEventArgs { Title = title, Content = content });
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ---- GỬI CHO 1 USER ----
        public async Task<bool> SendNotificationToUserAsync(int senderUserId, int receiverUserId, string title, string content)
        {
            try
            {
                var notification = new Notification
                {
                    Title = title,
                    Content = content,
                    SenderUserId = senderUserId,
                    ReceiverUserId = receiverUserId,
                    DepartmentId = null,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                await _context.Notifications.AddAsync(notification);

                // Thêm 1 bản sao cho người gửi nếu người gửi khác người nhận
                if (receiverUserId != senderUserId)
                {
                    await _context.Notifications.AddAsync(new Notification
                    {
                        Title = title,
                        Content = content,
                        SenderUserId = senderUserId,
                        ReceiverUserId = senderUserId,
                        DepartmentId = null,
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });
                }

                await _context.SaveChangesAsync();

                await LogNotificationAsync(senderUserId, title, "1 nhân viên", 1);
                OnNotificationReceived(new NotificationEventArgs { Title = title, Content = content });
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ---- LẤY DANH SÁCH THÔNG BÁO ----
        // includeSent: nếu true => trả cả thông báo do user gửi đi (Outbox) lẫn nhận (Inbox)
        public async Task<List<NotificationDto>> GetNotificationsAsync(int userId, int pageSize = 20, int pageNumber = 1, bool includeSent = true)
        {
            var query = _context.Notifications
                .AsNoTracking()
                .Include(n => n.SenderUser).ThenInclude(u => u.Employee)
                .Include(n => n.Department)
                .Where(n => n.ReceiverUserId == userId
                         || (includeSent && n.SenderUserId == userId));

            var list = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationDto
                {
                    NotificationId = n.NotificationId,
                    Title = n.Title,
                    Content = n.Content,
                    SenderName = n.SenderUser != null && n.SenderUser.Employee != null
                        ? n.SenderUser.Employee.FullName
                        : (n.SenderUser != null ? n.SenderUser.Email : "Hệ thống"),
                    DepartmentName = n.Department != null ? n.Department.DepartmentName : "Toàn bộ",
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return list;
        }

        public async Task<List<NotificationDto>> GetUnreadNotificationsAsync(int userId)
        {
            var list = await _context.Notifications
                .AsNoTracking()
                .Include(n => n.SenderUser).ThenInclude(u => u.Employee)
                .Include(n => n.Department)
                .Where(n => n.ReceiverUserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto
                {
                    NotificationId = n.NotificationId,
                    Title = n.Title,
                    Content = n.Content,
                    SenderName = n.SenderUser != null && n.SenderUser.Employee != null
                        ? n.SenderUser.Employee.FullName
                        : (n.SenderUser != null ? n.SenderUser.Email : "Hệ thống"),
                    DepartmentName = n.Department != null ? n.Department.DepartmentName : "Toàn bộ",
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return list;
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(notificationId);
                if (notification == null) return false;
                notification.IsRead = true;
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => n.ReceiverUserId == userId && !n.IsRead)
                    .ToListAsync();

                foreach (var n in notifications) n.IsRead = true;
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteNotificationAsync(int notificationId)
        {
            try
            {
                var n = await _context.Notifications.FindAsync(notificationId);
                if (n == null) return false;
                _context.Notifications.Remove(n);
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .AsNoTracking()
                .CountAsync(n => n.ReceiverUserId == userId && !n.IsRead);
        }

        protected virtual void OnNotificationReceived(NotificationEventArgs e)
            => NotificationReceived?.Invoke(this, e);

        private async Task LogNotificationAsync(int senderUserId, string title, string scopeDescription, int totalReceiver)
        {
            try
            {
                var detail = $"Gửi thông báo \"{title}\" tới {scopeDescription} (số người nhận: {totalReceiver})";
                await _context.ActivityLogs.AddAsync(new ActivityLog
                {
                    UserId = senderUserId,
                    Action = "Notify",
                    EntityName = nameof(Notification),
                    Details = detail,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
            catch
            {
                // ignore logging failure
            }
        }
    }

    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = "";
        public string? Content { get; set; }
        public string SenderName { get; set; } = "";
        public string DepartmentName { get; set; } = "Toàn bộ";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationEventArgs : EventArgs
    {
        public string Title { get; set; } = "";
        public string? Content { get; set; }
    }
}
