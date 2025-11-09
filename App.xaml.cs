using System.Windows;

namespace SmartPOS.UI
{
    public partial class App : Application
    {
private void Application_Startup(object sender, StartupEventArgs e)
{
    try
    {
        var mainWindow = new MainWindow();
        mainWindow.Show();
    }
    catch (Exception ex)
    {
        string path = System.IO.Path.Combine(
            System.AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");

        System.IO.File.WriteAllText(path, ex.ToString());

        MessageBox.Show(
            "SmartPOS crashed during startup.\n\n" +
            "Error details were written to:\n" + path,
            "SmartPOS Crash");
    }
}
    }
}
