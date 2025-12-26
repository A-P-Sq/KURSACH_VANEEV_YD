using System.Windows;

namespace AutoShopCoursework
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Сначала создаем и показываем окно авторизации
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
        }
    }
}