// ĐỔI namespace theo project của bạn
using ManagementEmployee.Models; // Chứa ManagementEmployeeContext
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ManagementEmployee
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // Tự kiểm tra khi mở cửa sổ (optional)
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await RunDbCheckAsync();
        }

        private async void btnTest_Click(object sender, RoutedEventArgs e)
        {
            await RunDbCheckAsync();
        }

        private async Task RunDbCheckAsync()
        {
            SetBusy(true, "Đang kiểm tra kết nối cơ sở dữ liệu...");
            try
            {
                // 1) Ưu tiên đọc connection string từ appsettings.json (nếu bạn dùng)
                string cs = TryGetConnectionStringFromAppSettings("ManagementEmployee");

                // 2) Nếu chưa có thì mượn connection string từ DbContext
                if (string.IsNullOrWhiteSpace(cs))
                {
                    using var tmp = new ManagementEmployeeContext();
                    cs = tmp.Database.GetDbConnection().ConnectionString;
                }

                // 3) Kiểm tra bằng raw SqlConnection để báo lỗi chi tiết nhất
                var (okSql, messageSql) = await CheckBySqlConnectionAsync(cs);

                // 4) (Tùy chọn) Kiểm tra thêm bằng EF Core Database.CanConnect()
                bool okEf = await CheckByEfCoreAsync();

                if (okSql && okEf)
                {
                    SetOk($"Kết nối thành công đến DB 'ManagementEmployee'.", messageSql);
                }
                else
                {
                    string details = $"Raw SQL: {(okSql ? "OK" : "FAIL")} | EF: {(okEf ? "OK" : "FAIL")}\n{messageSql}";
                    SetFail("Không thể kết nối cơ sở dữ liệu.", details);
                }
            }
            catch (Exception ex)
            {
                SetFail("Lỗi kiểm tra kết nối.", ex.Message);
            }
            finally
            {
                SetBusy(false);
            }
        }

        /// <summary>
        /// Đọc connection string "ManagementEmployee" từ appsettings.json (nếu có).
        /// appsettings.json nhớ đặt "Copy to Output Directory" = Copy always.
        /// </summary>
        private static string TryGetConnectionStringFromAppSettings(string name)
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var builder = new ConfigurationBuilder()
                    .SetBasePath(baseDir)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfiguration config = builder.Build();
                return config.GetConnectionString(name);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Kiểm tra bằng EF Core (Database.CanConnect).
        /// </summary>
        private static async Task<bool> CheckByEfCoreAsync()
        {
            try
            {
                using var db = new ManagementEmployeeContext();
                return await db.Database.CanConnectAsync();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Kiểm tra bằng SqlConnection + SELECT 1, trả về (ok, thông báo chi tiết).
        /// </summary>
        private static async Task<(bool ok, string message)> CheckBySqlConnectionAsync(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return (false, "Connection string rỗng. Kiểm tra appsettings.json & OnConfiguring.");

            try
            {
                using var conn = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT DB_NAME() AS DbName, @@SERVERNAME AS ServerName;";
                using var reader = await cmd.ExecuteReaderAsync();
                string info = "";
                if (await reader.ReadAsync())
                    info = $"Server: {reader["ServerName"]} | Database: {reader["DbName"]}";

                return (true, $"Kết nối SQL thành công. {info}");
            }
            catch (Microsoft.Data.SqlClient.SqlException sx)
            {
                // Bắt chi tiết để khoanh vùng nhanh
                var sb = new StringBuilder();
                sb.AppendLine($"SqlException Number={sx.Number}, State={sx.State}, Class={sx.Class}");
                sb.AppendLine(sx.Message);
                if (sx.InnerException != null) sb.AppendLine("Inner: " + sx.InnerException.Message);

                // Một số gợi ý theo mã lỗi
                switch (sx.Number)
                {
                    case 26: sb.AppendLine("▶ Không tìm thấy Server/Instance. Kiểm tra 'Server=...'(., .\\SQLEXPRESS, (localdb)\\MSSQLLocalDB)."); break;
                    case 53: sb.AppendLine("▶ Không kết nối được máy chủ (service chưa chạy / hostname sai / firewall)."); break;
                    case 18456: sb.AppendLine("▶ Sai user/mật khẩu hoặc không được phép (SQL Authentication)."); break;
                    case 4060: sb.AppendLine("▶ Database không tồn tại/không truy cập được. Kiểm tra tên DB 'ManagementEmployee'."); break;
                }
                return (false, sb.ToString());
            }
            catch (Exception ex)
            {
                return (false, $"Exception: {ex.Message}\n{ex.InnerException?.Message}");
            }
        }

        /* ---------------- UI helpers ---------------- */

        private void SetBusy(bool isBusy, string statusText = null)
        {
            bar.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
            bar.IsIndeterminate = isBusy;

            if (!string.IsNullOrEmpty(statusText))
            {
                txtStatus.Text = "Status: " + statusText;
                dot.Fill = new SolidColorBrush(Colors.DarkGray);
                txtDetail.Text = "";
            }
        }

        private void SetOk(string status, string detail = "")
        {
            txtStatus.Text = "Status: " + status;
            dot.Fill = new SolidColorBrush(Colors.LimeGreen);
            txtDetail.Text = string.IsNullOrWhiteSpace(detail) ? "OK" : detail;
        }

        private void SetFail(string status, string detail = "")
        {
            txtStatus.Text = "Status: " + status;
            dot.Fill = new SolidColorBrush(Colors.OrangeRed);
            txtDetail.Text = detail;
        }

    }
}
