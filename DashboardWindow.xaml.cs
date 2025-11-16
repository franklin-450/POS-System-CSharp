using System.Windows;
using SmartPOS.UI.Helpers;

namespace SmartPOS.UI
{
    public partial class DashboardWindow : Window
    {
        private readonly string _adminName;

        public DashboardWindow(string adminName)
        {
            InitializeComponent();
            _adminName = adminName;
            AdminNameText.Text = adminName;
        }
        private void OpenCashierWindow_Click(object sender, RoutedEventArgs e)
{
    var win = new CashierWindow();
    win.Show();
}

        private void GoHome_Click(object sender, RoutedEventArgs e)
        {
            MainWindow home = new MainWindow();
            home.Show();
            this.Close();
        }

        private void OpenProducts_Click(object sender, RoutedEventArgs e)
        {
            ProductEntryWindow win = new ProductEntryWindow();
            win.Show();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            LocalStorage.Set("admin_user", ""); // clear saved session

            AdminLoginWindow login = new AdminLoginWindow();
            login.Show();

            this.Close();
        }
    }
}
