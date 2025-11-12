using ManagementEmployee.Models;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagementEmployee.Services
{
    public class ReportService
    {
        private static string DocsDir => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        private static string WriteCsv(string fileName, string header, string[][] rows)
        {
            var path = Path.Combine(DocsDir, fileName);
            var sb = new StringBuilder();
            sb.AppendLine(header);
            foreach (var r in rows)
                sb.AppendLine(string.Join(",", r.Select(cell => Escape(cell))));
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return path;
        }

        private static string Escape(string s)
        {
            if (s == null) return "";
            if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        // =================== Public APIs used by VM ===================

        // Excel -> dùng CSV để Excel mở trực tiếp
        public async Task<string> ExportEmployeeByDepartmentExcelAsync()
        {
            return await Task.Run(async () =>
            {
                var stat = await new StatisticService().GetEmployeeByDepartmentAsync();
                var rows = stat.Select(x => new[]
                {
                    x.DepartmentName,
                    x.TotalEmployees.ToString(),
                    x.InactiveEmployees.ToString(),
                    x.Total.ToString(),
                    x.ActiveRatio.ToString("F1", CultureInfo.InvariantCulture),
                    x.InactiveRatio.ToString("F1", CultureInfo.InvariantCulture)
                }).ToArray();

                return WriteCsv($"Employees_By_Department_{DateTime.Now:yyyyMMdd_HHmm}.csv",
                    "Department,Active,Inactive,Total,ActivePercent,InactivePercent", rows);
            });
        }

        public async Task<string> ExportEmployeeByPositionExcelAsync()
        {
            return await Task.Run(async () =>
            {
                var stat = await new StatisticService().GetEmployeeByPositionAsync();
                var rows = stat.Select(x => new[]
                {
                    x.Position,
                    x.Count.ToString(),
                    x.AverageSalary.ToString("N0", CultureInfo.InvariantCulture)
                }).ToArray();

                return WriteCsv($"Employees_By_Position_{DateTime.Now:yyyyMMdd_HHmm}.csv",
                    "Position,Count,AverageSalary", rows);
            });
        }

        public async Task<string> ExportSalaryByMonthExcelAsync(int year)
        {
            return await Task.Run(async () =>
            {
                var stat = await new StatisticService().GetSalaryByMonthAsync(year);
                var rows = stat.Select(x => new[]
                {
                    x.Month.ToString(),
                    x.TotalEmployees.ToString(),
                    x.TotalGross.ToString("N0", CultureInfo.InvariantCulture),
                    x.TotalNet.ToString("N0", CultureInfo.InvariantCulture),
                    x.AverageGross.ToString("N0", CultureInfo.InvariantCulture),
                    x.AverageNet.ToString("N0", CultureInfo.InvariantCulture)
                }).ToArray();

                return WriteCsv($"Salary_By_Month_{year}_{DateTime.Now:yyyyMMdd_HHmm}.csv",
                    "Month,Employees,TotalGross,TotalNet,AvgGross,AvgNet", rows);
            });
        }

        public async Task<string> ExportSalaryByQuarterExcelAsync(int year)
        {
            return await Task.Run(async () =>
            {
                var stat = await new StatisticService().GetSalaryByQuarterAsync(year);
                var rows = stat.Select(x => new[]
                {
                    x.Quarter.ToString(),
                    x.TotalGross.ToString("N0", CultureInfo.InvariantCulture),
                    x.TotalNet.ToString("N0", CultureInfo.InvariantCulture),
                    x.AverageGross.ToString("N0", CultureInfo.InvariantCulture),
                    x.AverageNet.ToString("N0", CultureInfo.InvariantCulture),
                    x.MonthCount.ToString()
                }).ToArray();

                return WriteCsv($"Salary_By_Quarter_{year}_{DateTime.Now:yyyyMMdd_HHmm}.csv",
                    "Quarter,TotalGross,TotalNet,AvgGross,AvgNet,MonthCount", rows);
            });
        }

        // PDF -> đơn giản tạo HTML để in ra, bạn có thể thay thế bằng thư viện PDF sau
        public async Task<string> ExportSalaryByMonthPdfAsync(int year)
        {
            var html = await BuildSimpleSalaryHtmlByMonth(year);
            var path = Path.Combine(DocsDir, $"Salary_By_Month_{year}_{DateTime.Now:yyyyMMdd_HHmm}.html");
            File.WriteAllText(path, html, Encoding.UTF8);
            return path;
        }

        public async Task<string> ExportSalaryByQuarterPdfAsync(int year)
        {
            var html = await BuildSimpleSalaryHtmlByQuarter(year);
            var path = Path.Combine(DocsDir, $"Salary_By_Quarter_{year}_{DateTime.Now:yyyyMMdd_HHmm}.html");
            File.WriteAllText(path, html, Encoding.UTF8);
            return path;
        }

        private static async Task<string> BuildSimpleSalaryHtmlByMonth(int year)
        {
            var stat = await new StatisticService().GetSalaryByMonthAsync(year);
            var sb = new StringBuilder();
            sb.AppendLine("<html><head><meta charset='utf-8'><title>Bao cao luong theo thang</title></head><body>");
            sb.AppendLine($"<h2>Luong theo thang - {year}</h2><table border='1' cellspacing='0' cellpadding='6'>");
            sb.AppendLine("<tr><th>Thang</th><th>Nhan su</th><th>Total Gross</th><th>Total Net</th><th>Gross TB</th><th>Net TB</th></tr>");
            foreach (var r in stat)
                sb.AppendLine($"<tr><td>{r.Month}</td><td>{r.TotalEmployees}</td><td>{r.TotalGross:N0}</td><td>{r.TotalNet:N0}</td><td>{r.AverageGross:N0}</td><td>{r.AverageNet:N0}</td></tr>");
            sb.AppendLine("</table></body></html>");
            return sb.ToString();
        }

        private static async Task<string> BuildSimpleSalaryHtmlByQuarter(int year)
        {
            var stat = await new StatisticService().GetSalaryByQuarterAsync(year);
            var sb = new StringBuilder();
            sb.AppendLine("<html><head><meta charset='utf-8'><title>Bao cao luong theo quy</title></head><body>");
            sb.AppendLine($"<h2>Luong theo quy - {year}</h2><table border='1' cellspacing='0' cellpadding='6'>");
            sb.AppendLine("<tr><th>Quy</th><th>Total Gross</th><th>Total Net</th><th>Gross TB</th><th>Net TB</th><th>So thang</th></tr>");
            foreach (var r in stat)
                sb.AppendLine($"<tr><td>Q{r.Quarter}</td><td>{r.TotalGross:N0}</td><td>{r.TotalNet:N0}</td><td>{r.AverageGross:N0}</td><td>{r.AverageNet:N0}</td><td>{r.MonthCount}</td></tr>");
            sb.AppendLine("</table></body></html>");
            return sb.ToString();
        }
    }
}
