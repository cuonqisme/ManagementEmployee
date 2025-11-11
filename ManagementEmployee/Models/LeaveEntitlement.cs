using System;
using System.Collections.Generic;

namespace ManagementEmployee.Models;

public partial class LeaveEntitlement
{
    public int EntitlementId { get; set; }

    public int EmployeeId { get; set; }

    public int LeaveTypeId { get; set; }

    public short Year { get; set; }

    public decimal EntitledDays { get; set; }

    public decimal CarriedOver { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual LeaveType LeaveType { get; set; } = null!;
}
