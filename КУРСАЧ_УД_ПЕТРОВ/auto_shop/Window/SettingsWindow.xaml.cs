using System;
using System.Windows;

namespace auto_shop.Window
{
    public partial class SettingsWindow : System.Windows.Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            try
            {
                // Используем полное имя проекта auto_shop
                string currentConn = auto_shop.Properties.Settings.Default.ConnectionString;

                if (!string.IsNullOrEmpty(currentConn))
                {
                    var parts = currentConn.Split(';');
                    foreach (var part in parts)
                    {
                        if (part.StartsWith("Host=")) txtHost.Text = part.Replace("Host=", "");
                        if (part.StartsWith("Port=")) txtPort.Text = part.Replace("Port=", "");
                        if (part.StartsWith("Username=")) txtUser.Text = part.Replace("Username=", "");
                        if (part.StartsWith("Password=")) txtPass.Password = part.Replace("Password=", "");
                        if (part.StartsWith("Database=")) txtDb.Text = part.Replace("Database=", "");
                    }
                }
            }
            catch { }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtHost.Text) || string.IsNullOrWhiteSpace(txtDb.Text))
            {
                MessageBox.Show("Заполните Хост и Базу данных!");
                return;
            }

            string connectionString = $"Host={txtHost.Text};Port={txtPort.Text};Username={txtUser.Text};Password={txtPass.Password};Database={txtDb.Text}";

            try
            {
                // Сохраняем через полное пространство имен
                auto_shop.Properties.Settings.Default.ConnectionString = connectionString;
                auto_shop.Properties.Settings.Default.Save();

                MessageBox.Show("Настройки успешно сохранены!");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
    }
}