using System;
using System.Collections.Generic;

namespace ManagementEmployee.Models;

public partial class PayrollAdjustment
{
    public int AdjustmentId { get; set; }

    public int PayrollId { get; set; }

    public string AdjType { get; set; } = null!;

    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Payroll Payroll { get; set; } = null!;
}
