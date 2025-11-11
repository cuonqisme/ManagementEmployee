using System;
using System.Collections.Generic;

namespace ManagementEmployee.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public string Title { get; set; } = null!;

    public string? Content { get; set; }

    public int? SenderUserId { get; set; }

    public int? ReceiverUserId { get; set; }

    public int? DepartmentId { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Department? Department { get; set; }

    public virtual User? ReceiverUser { get; set; }

    public virtual User? SenderUser { get; set; }
}
