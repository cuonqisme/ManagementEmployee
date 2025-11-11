using System;
using System.Collections.Generic;

namespace ManagementEmployee.Models;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public int EmployeeId { get; set; }

    public DateOnly WorkDate { get; set; }

    public DateTime? CheckIn { get; set; }

    public DateTime? CheckOut { get; set; }

    public decimal WorkHours { get; set; }

    public decimal OvertimeHours { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
