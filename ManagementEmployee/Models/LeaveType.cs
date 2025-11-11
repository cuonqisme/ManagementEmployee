using System;
using System.Collections.Generic;

namespace ManagementEmployee.Models;

public partial class LeaveType
{
    public int LeaveTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public decimal DefaultDays { get; set; }

    public bool IsPaid { get; set; }

    public virtual ICollection<LeaveEntitlement> LeaveEntitlements { get; set; } = new List<LeaveEntitlement>();

    public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
}
