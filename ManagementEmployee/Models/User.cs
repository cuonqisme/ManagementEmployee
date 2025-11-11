using System;
using System.Collections.Generic;

namespace ManagementEmployee.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? PasswordSalt { get; set; }

    public int RoleId { get; set; }

    public int? EmployeeId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

    public virtual Employee? Employee { get; set; }

    public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();

    public virtual ICollection<Notification> NotificationReceiverUsers { get; set; } = new List<Notification>();

    public virtual ICollection<Notification> NotificationSenderUsers { get; set; } = new List<Notification>();

    public virtual Role Role { get; set; } = null!;
}
