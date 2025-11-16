using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media;
using SmartPOS.UI.Models;

namespace SmartPOS.UI
{
    public partial class CashierWindow : Window
    {
        private ObservableCollection<Cashier> Cashiers = new ObservableCollection<Cashier>();

        public CashierWindow()
{
    InitializeComponent();
    CashierList.ItemsSource = SmartPOS.UI.Services.CashierService.GetAll();
}

private void BtnAddCashier_Click(object sender, RoutedEventArgs e)
{
    string name = NameBox.Text.Trim();
    string username = UsernameBox.Text.Trim();

    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(username))
    {
        ShowNotification("Name and username are required!", "#E53935");
        return;
    }

    string password = GeneratePassword(8);

    Cashier cashier = new Cashier
    {
        Name = name,
        Username = username,
        Password = password
    };

    // Save permanently
    SmartPOS.UI.Services.CashierService.Add(cashier);

    // Refresh ListView
    CashierList.ItemsSource = null;
    CashierList.ItemsSource = SmartPOS.UI.Services.CashierService.GetAll();

    // Clear inputs
    NameBox.Text = "";
    UsernameBox.Text = "";
    PasswordBox.Password = password;

    ShowNotification($"Cashier added! Password: {password}", "#43A047");
}

        private string GeneratePassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@#$!";
            Random random = new Random();
            char[] buffer = new char[length];
            for (int i = 0; i < length; i++)
            {
                buffer[i] = chars[random.Next(chars.Length)];
            }
            return new string(buffer);
        }

        private async void ShowNotification(string message, string color)
        {
            NotificationText.Text = message;
            NotificationText.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(color);

            // Fade in
            DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400));
            NotificationText.BeginAnimation(OpacityProperty, fadeIn);

            // Wait
            await Task.Delay(2500);

            // Fade out
            DoubleAnimation fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(400));
            NotificationText.BeginAnimation(OpacityProperty, fadeOut);
        }

    }

    // Model class
    public class Cashier
    {
        public string Name { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
