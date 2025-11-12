using ManagementEmployee.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManagementEmployee.Services
{
    public class StatisticService
    {
        private ManagementEmployeeContext NewDb() => new ManagementEmployeeContext();

        public async Task<List<DepartmentStatistic>> GetEmployeeByDepartmentAsync()
        {
            using var db = NewDb();

            var deptNames = await db.Departments.AsNoTracking()
                                .Select(d => new { d.DepartmentId, d.DepartmentName })
                                .ToListAsync();

            var empGrouped = await db.Employees.AsNoTracking()
                                .GroupBy(e => e.DepartmentId)
                                .Select(g => new
                                {
                                    DepartmentId = g.Key,
                                    Active = g.Count(e => e.IsActive),
                                    Inactive = g.Count(e => !e.IsActive)
                                }).ToListAsync();

            var result = from d in deptNames
                         join g in empGrouped on d.DepartmentId equals g.DepartmentId into gj
                         from x in gj.DefaultIfEmpty()
                         select new DepartmentStatistic
                         {
                             DepartmentName = d.DepartmentName,
                             TotalEmployees = x?.Active ?? 0,
                             InactiveEmployees = x?.Inactive ?? 0
                         };

            return result.OrderBy(r => r.DepartmentName).ToList();
        }

        public async Task<List<PositionStatistic>> GetEmployeeByPositionAsync()
        {
            using var db = NewDb();
            var q = await db.Employees.AsNoTracking()
                        .GroupBy(e => e.Position ?? "")
                        .Select(g => new PositionStatistic
                        {
                            Position = g.Key,
                            Count = g.Count(),
                            AverageSalary = g.Average(e => (decimal?)e.BaseSalary) ?? 0m
                        })
                        .OrderByDescending(x => x.Count)
                        .ToListAsync();
            return q;
        }

        public async Task<List<GenderStatistic>> GetEmployeeByGenderAsync()
        {
            using var db = NewDb();
            var map = new Dictionary<string, string> { ["M"] = "Nam", ["F"] = "Nữ", ["O"] = "Khác" };

            var q = await db.Employees.AsNoTracking()
                        .GroupBy(e => e.Gender ?? "")
                        .Select(g => new GenderStatistic
                        {
                            Gender = g.Key,
                            Count = g.Count()
                        }).ToListAsync();

            foreach (var item in q)
            {
                item.Gender = map.TryGetValue(item.Gender ?? "", out var name) ? name : "Không rõ";
            }
            return q.OrderByDescending(x => x.Count).ToList();
        }

        public async Task<List<int>> GetAvailableYearsAsync()
        {
            using var db = NewDb();
            return await db.Payrolls.AsNoTracking()
                        .Select(p => (int)p.PeriodYear)
                        .Distinct()
                        .OrderByDescending(y => y)
                        .ToListAsync();
        }

        public async Task<List<MonthlySalaryStatistic>> GetSalaryByMonthAsync(int year)
        {
            using var db = NewDb();

            var rows = await db.Payrolls.AsNoTracking()
                        .Where(p => p.PeriodYear == year)
                        .GroupBy(p => p.PeriodMonth)
                        .Select(g => new
                        {
                            Month = (int)g.Key,
                            Emp = g.Count(),
                            TotalGross = g.Sum(p => (decimal?)(p.Gross ?? (p.BasicSalary + p.OvertimePay + p.TotalAllowance + p.TotalBonus - p.TotalPenalty))) ?? 0m,
                            TotalDeduct = g.Sum(p => (decimal?)p.TotalDeduction) ?? 0m,
                            TotalNet = g.Sum(p => (decimal?)((p.Gross ?? (p.BasicSalary + p.OvertimePay + p.TotalAllowance + p.TotalBonus - p.TotalPenalty)) - p.TotalDeduction)) ?? 0m
                        })
                        .ToListAsync();

            return rows.OrderBy(r => r.Month)
                       .Select(r => new MonthlySalaryStatistic
                       {
                           Month = r.Month,
                           TotalEmployees = r.Emp,
                           TotalGross = r.TotalGross,
                           TotalNet = r.TotalNet,
                           AverageGross = r.Emp == 0 ? 0 : r.TotalGross / r.Emp,
                           AverageNet = r.Emp == 0 ? 0 : r.TotalNet / r.Emp
                       }).ToList();
        }

        public async Task<List<QuarterlySalaryStatistic>> GetSalaryByQuarterAsync(int year)
        {
            var months = await GetSalaryByMonthAsync(year);

            var qGroups = months.GroupBy(m => (m.Month - 1) / 3 + 1)
                                .Select(g => new QuarterlySalaryStatistic
                                {
                                    Quarter = g.Key,
                                    TotalGross = g.Sum(x => x.TotalGross),
                                    TotalNet = g.Sum(x => x.TotalNet),
                                    AverageGross = g.Sum(x => x.TotalEmployees) == 0 ? 0 : g.Sum(x => x.TotalGross) / g.Sum(x => x.TotalEmployees),
                                    AverageNet = g.Sum(x => x.TotalEmployees) == 0 ? 0 : g.Sum(x => x.TotalNet) / g.Sum(x => x.TotalEmployees),
                                    MonthCount = g.Count()
                                })
                                .OrderBy(x => x.Quarter)
                                .ToList();
            return qGroups;
        }
    }
}
