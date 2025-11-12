namespace ManagementEmployee.Models
{
    public class DepartmentStatistic
    {
        public string DepartmentName { get; set; } = "";
        public int TotalEmployees { get; set; }          // active
        public int InactiveEmployees { get; set; }
        public int Total => TotalEmployees + InactiveEmployees;
        public double ActiveRatio => Total == 0 ? 0 : (double)TotalEmployees * 100.0 / Total;
        public double InactiveRatio => Total == 0 ? 0 : (double)InactiveEmployees * 100.0 / Total;
    }

    public class PositionStatistic
    {
        public string Position { get; set; } = "";
        public int Count { get; set; }
        public decimal AverageSalary { get; set; }
    }

    public class GenderStatistic
    {
        public string Gender { get; set; } = "";
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class MonthlySalaryStatistic
    {
        public int Month { get; set; }
        public int TotalEmployees { get; set; }
        public decimal TotalGross { get; set; }
        public decimal TotalNet { get; set; }
        public decimal AverageGross { get; set; }
        public decimal AverageNet { get; set; }
    }

    public class QuarterlySalaryStatistic
    {
        public int Quarter { get; set; }
        public decimal TotalGross { get; set; }
        public decimal TotalNet { get; set; }
        public decimal AverageGross { get; set; }
        public decimal AverageNet { get; set; }
        public int MonthCount { get; set; }
    }
}
