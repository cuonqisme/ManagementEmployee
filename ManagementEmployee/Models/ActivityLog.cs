using System;
using System.Collections.Generic;

namespace ManagementEmployee.Models;

public partial class ActivityLog
{
    public long ActivityId { get; set; }

    public int? UserId { get; set; }

    public string Action { get; set; } = null!;

    public string? EntityName { get; set; }

    public long? EntityId { get; set; }

    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
