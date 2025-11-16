using SmartPOS.UI.Helpers;
using System;
using System.Collections.Generic;
using System.Windows;

namespace SmartPOS.UI
{
    public partial class AdminLoginWindow : Window
    {
        private readonly Dictionary<string, string> Admins = new()
        {
            { "admin", "password123" },
            { "manager", "pass@456" },
            { "ceo", "ceo2025" }
        };

        public AdminLoginWindow()
        {
            InitializeComponent();

            // 🚀 Auto-redirect if login is saved
            string lastUser = LocalStorage.Get("admin_user");
            if (!string.IsNullOrEmpty(lastUser))
            {
                DashboardWindow dashboard = new DashboardWindow(lastUser);
                dashboard.Show();
                this.Close();
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password.Trim();

            if (!Admins.ContainsKey(username) || Admins[username] != password)
            {
                ErrorText.Text = "❌ Invalid username or password!";
                return;
            }

            // Save the logged admin
           LocalStorage.Set("admin_user", username);

            DashboardWindow dashboard = new DashboardWindow(username);
            dashboard.Show();

            this.Close();
        }
    }
}
