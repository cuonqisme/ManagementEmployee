using ManagementEmployee.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ManagementEmployee.Models;

namespace ManagementEmployee
{
    /// <summary>
    /// Interaction logic for RegisterWindow.xaml
    /// </summary>
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try { if (e.ChangedButton == MouseButton.Left) DragMove(); } catch { }
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            var login = new LoginWindow();
            login.Show();
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try { if (e.ChangedButton == MouseButton.Left) DragMove(); } catch { }
        }

        private void Button_Save(object sender, RoutedEventArgs e)
        {
            string fullName = (FindName("txtFullName") as dynamic)?.Text?.Trim();
            string email = (FindName("txtEmail") as dynamic)?.Text?.Trim();
            string password = (FindName("txtPassword") as dynamic)?.Text ?? string.Empty;
            string phone = (FindName("txtPhone") as dynamic)?.Text?.Trim();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Vui lòng nhập Email và Mật khẩu.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!IsValidEmail(email))
            {
                MessageBox.Show("Email không hợp lệ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (password.Length < 6)
            {
                MessageBox.Show("Mật khẩu tối thiểu 6 ký tự.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (var db = new ManagementEmployeeContext()) // ĐỔI tên DbContext nếu khác
                {
                    // RoleId cho Employee
                    var employeeRoleId = db.Roles
                        .Where(r => r.RoleName == "Employee")
                        .Select(r => r.RoleId)
                        .FirstOrDefault();

                    if (employeeRoleId == 0)
                    {
                        // Seed nhanh nếu thiếu
                        var r = new Role { RoleName = "Employee" };
                        db.Roles.Add(r);
                        db.SaveChanges();
                        employeeRoleId = r.RoleId;
                    }

                    // Check trùng email
                    bool exists = db.Users.Any(u => u.Email == email);
                    if (exists)
                    {
                        MessageBox.Show("Email đã tồn tại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Tạo salt + hash
                    string salt = CreateSalt(16);                         // hex 16 byte
                    string hash = ComputeSha256(salt + password);        // SHA256(salt + password)

                    var user = new User
                    {
                        Email = email,
                        PasswordHash = hash,
                        PasswordSalt = salt,
                        RoleId = employeeRoleId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    db.Users.Add(user);
                    db.SaveChanges();

                    MessageBox.Show("Đăng ký thành công! Vui lòng đăng nhập.", "Thành công",
                                    MessageBoxButton.OK, MessageBoxImage.Information);

                    var login = new LoginWindow();
                    login.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi đăng ký: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button_Cancel(object sender, RoutedEventArgs e)
        {
            var login = new LoginWindow();
            login.Show();
            this.Close();
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            var pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            return Regex.IsMatch(email, pattern);
        }

        private static string CreateSalt(int bytes = 16)
        {
            using var rng = RandomNumberGenerator.Create();
            var data = new byte[bytes];
            rng.GetBytes(data);
            var sb = new StringBuilder(bytes * 2);
            foreach (var b in data) sb.AppendFormat("{0:x2}", b);
            return sb.ToString(); // hex
        }

        private static string ComputeSha256(string raw)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.AppendFormat("{0:x2}", b);
            return sb.ToString(); // hex
        }
    }
}
