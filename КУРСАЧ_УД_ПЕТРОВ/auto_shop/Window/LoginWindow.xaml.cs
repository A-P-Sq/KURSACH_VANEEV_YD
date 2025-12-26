using auto_shop.Window;
using Npgsql;
using System.Windows;

namespace AutoShopCoursework
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settings = new SettingsWindow();
            settings.Owner = this;
            settings.ShowDialog();
        }
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text;
            string pass = txtPass.Password;

            // Вызов функции авторизации
            string query = "SELECT * FROM public.auth_user(@l, @p)";
            var parameters = new NpgsqlParameter[] {
                new NpgsqlParameter("l", login),
                new NpgsqlParameter("p", pass)
            };

            var dt = DbHelper.GetTable(query, parameters);

            if (dt.Rows.Count > 0)
            {
                string role = dt.Rows[0]["role_name"].ToString();
                int userId = Convert.ToInt32(dt.Rows[0]["user_id"]);

                MainWindow main = new MainWindow(role, userId);
                main.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль!");
            }
        }
    }
}