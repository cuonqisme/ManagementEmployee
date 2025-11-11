using System;
using System.Collections.Generic;

namespace ManagementEmployee.Models;

public partial class Payroll
{
    public int PayrollId { get; set; }

    public int EmployeeId { get; set; }

    public short PeriodYear { get; set; }

    public byte PeriodMonth { get; set; }

    public decimal BasicSalary { get; set; }

    public decimal OvertimePay { get; set; }

    public decimal TotalAllowance { get; set; }

    public decimal TotalBonus { get; set; }

    public decimal TotalPenalty { get; set; }

    public decimal TotalDeduction { get; set; }

    public decimal? Gross { get; set; }

    public decimal? Net { get; set; }

    public DateOnly? PayDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual ICollection<PayrollAdjustment> PayrollAdjustments { get; set; } = new List<PayrollAdjustment>();
}
