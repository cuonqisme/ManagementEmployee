using ManagementEmployee.Models;
using ManagementEmployee.View.Admin;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ManagementEmployee
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUser.Text?.Trim();
            string password = txtPass.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ Email và Mật khẩu.", "Thiếu thông tin",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var db = new ManagementEmployeeContext())
                {
                    string uname = username.ToLower();
                    var user = await db.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Email.ToLower() == uname);

                    if (user == null)
                    {
                        MessageBox.Show("Tài khoản không tồn tại.", "Đăng nhập thất bại",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (user.IsActive == false)
                    {
                        MessageBox.Show("Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ quản trị.",
                                        "Truy cập bị từ chối",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // hash pass => 
                    bool ok = VerifyPasswordFlexible(password, user.PasswordHash, user.PasswordSalt);
                    if (!ok)
                    {
                        MessageBox.Show("Mật khẩu không chính xác.", "Đăng nhập thất bại",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // activity log
                    await LogActivityAsync(user.UserId, "Login", "Users", user.UserId, "User login successfully");

                    //
                    switch (user.RoleId)
                    {
                        case 1:
                            {
                                var win = new AdminWindow();
                                Application.Current.MainWindow = win;
                                win.Show();
                                break;
                            }
                        case 2:
                            {
                                //var win = new EmployeeMainWindow(); 
                                //Application.Current.MainWindow = win;
                                //win.Show();
                                break;
                            }
                        default:
                            MessageBox.Show("Vai trò tài khoản không hợp lệ. Liên hệ quản trị để được cấp quyền.",
                                            "Lỗi phân quyền", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                    }

                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Có lỗi xảy ra khi đăng nhập: " + ex.Message,
                                "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void Register_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var registerWindow = new RegisterWindow();
            registerWindow.Show();
            this.Hide();
        }

        private static bool VerifyPasswordFlexible(string inputPassword, string storedHash, string storedSalt)
        {
            if (string.IsNullOrEmpty(storedHash))
                return false;

            // 1) salted SHA256 (Base64/Hex)
            if (!string.IsNullOrEmpty(storedSalt))
            {
                var salted = storedSalt + inputPassword;
                if (FixedTimeEquals(ToBase64(Sha256(salted)), storedHash)) return true;
                if (FixedTimeEquals(ToHex(Sha256(salted)), storedHash)) return true;
            }

            // 2) unsalted SHA256 (Base64/Hex)
            var raw = inputPassword;
            if (FixedTimeEquals(ToBase64(Sha256(raw)), storedHash)) return true;
            if (FixedTimeEquals(ToHex(Sha256(raw)), storedHash)) return true;

            // 3) plaintext fallback
            if (FixedTimeEquals(inputPassword, storedHash)) return true;

            return false;
        }

        private static byte[] Sha256(string s)
        {
            using (var sha = SHA256.Create())
                return sha.ComputeHash(Encoding.UTF8.GetBytes(s));
        }

        private static string ToBase64(byte[] bytes) => Convert.ToBase64String(bytes);

        private static string ToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString(); // lowercase hex
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        private static async Task LogActivityAsync(int userId, string action, string entity, long? entityId, string details)
        {
            try
            {
                using (var db = new ManagementEmployeeContext())
                {
                    var log = new ActivityLog
                    {
                        UserId = userId,
                        Action = action,
                        EntityName = entity,
                        EntityId = entityId,
                        Details = details,
                        CreatedAt = DateTime.UtcNow
                    };
                    db.ActivityLogs.Add(log);
                    await db.SaveChangesAsync();
                }
            }
            catch
            {
            }
        }
    }
}
