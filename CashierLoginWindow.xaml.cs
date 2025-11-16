using System.Windows;
using SmartPOS.UI.Helpers;
using SmartPOS.UI.Models;

namespace SmartPOS.UI
{
    public partial class CashierLoginWindow : Window
    {
        public string LoggedInCashier { get; private set; } = string.Empty;

        private readonly List<CashierModel> _cashiers;

        public CashierLoginWindow()
        {
            InitializeComponent();
            _cashiers = LocalStorage.LoadCashiers();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim();
            string password = ShowPasswordToggle.IsChecked == true
                ? PasswordTextBox.Text
                : PasswordBox.Password;

            var cashier = _cashiers.Find(c => c.Username == username && c.Password == password);

            if (cashier != null)
            {
                LoggedInCashier = cashier.Name;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageText.Text = "❌ Invalid username or password!";
                PasswordBox.Clear();
                PasswordTextBox.Clear();
            }
        }

        private void ShowPasswordToggle_Checked(object sender, RoutedEventArgs e)
        {
            PasswordTextBox.Text = PasswordBox.Password;
            PasswordTextBox.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;
        }

        private void ShowPasswordToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            PasswordBox.Password = PasswordTextBox.Text;
            PasswordBox.Visibility = Visibility.Visible;
            PasswordTextBox.Visibility = Visibility.Collapsed;
        }
    }
}
