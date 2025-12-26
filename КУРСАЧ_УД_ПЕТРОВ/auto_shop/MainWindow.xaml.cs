using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Npgsql;
using NpgsqlTypes;
using System.IO;
using System.Text;

namespace AutoShopCoursework
{
    public partial class MainWindow : Window
    {
        private string _userRole;
        private int _userId;

        public MainWindow(string role, int userId)
        {
            InitializeComponent();
            _userRole = role;
            _userId = userId;

            // Скрываем вкладку пользователей для обычных сотрудников
            if (_userRole != "admin" && TabUsers != null)
            {
                TabUsers.Visibility = Visibility.Collapsed;
            }

            LoadAllData();
        }

        public void LoadAllData()
        {
            try
            {
                // Загрузка данных для всех вкладок
                dgCars.ItemsSource = DbHelper.GetCarsByFilters(null, null, null).DefaultView;
                dgClients.ItemsSource = DbHelper.GetTable("SELECT * FROM clients ORDER BY id").DefaultView;
                dgSales.ItemsSource = DbHelper.GetTable("SELECT * FROM public.get_sales_with_details()").DefaultView;
                dgLog.ItemsSource = DbHelper.GetTable("SELECT * FROM sales_log ORDER BY log_date DESC").DefaultView;

                // Специфичные данные для администратора (Вкладка Пользователи)
                if (_userRole == "admin")
                {
                    string sqlUsers = @"SELECT u.id, u.login, u.password, r.name as role, u.role_id 
                                        FROM users u 
                                        JOIN roles r ON u.role_id = r.id 
                                        ORDER BY u.id";
                    dgUsers.ItemsSource = DbHelper.GetTable(sqlUsers).DefaultView;
                    cbUserRole.ItemsSource = DbHelper.GetTable("SELECT id, name FROM roles").DefaultView;
                }

                RefreshSalesComboBoxes();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при обновлении данных: " + ex.Message);
            }
        }

        // --- ЭКСПОРТ В CSV ---
        private void BtnExportCars_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgCars.ItemsSource == null) return;

                SaveFileDialog sfd = new SaveFileDialog()
                {
                    Filter = "CSV файл (*.csv)|*.csv",
                    FileName = $"Cars_Report_{DateTime.Now:dd_MM_yy}.csv"
                };

                if (sfd.ShowDialog() == true)
                {
                    DataView view = (DataView)dgCars.ItemsSource;
                    StringBuilder csv = new StringBuilder();

                    csv.AppendLine("ID;Марка;Модель;Год;Цена;Статус");

                    foreach (DataRowView row in view)
                    {
                        string soldStatus = Convert.ToBoolean(row["is_sold"]) ? "Продано" : "В продаже";
                        csv.AppendLine($"{row["id"]};{row["brand"]};{row["model"]};{row["year"]};{row["price"]};{soldStatus}");
                    }

                    File.WriteAllText(sfd.FileName, csv.ToString(), Encoding.UTF8);
                    MessageBox.Show("Экспорт завершен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при экспорте: " + ex.Message);
            }
        }

        // --- ВКЛАДКА АВТОМОБИЛИ ---

        private void BtnSearchCars_Click(object sender, RoutedEventArgs e)
        {
            string brand = string.IsNullOrWhiteSpace(txtSearchBrand.Text) ? null : txtSearchBrand.Text;
            string model = string.IsNullOrWhiteSpace(txtSearchModel.Text) ? null : txtSearchModel.Text;
            int? year = null;
            if (int.TryParse(txtSearchYear.Text, out int y)) year = y;

            dgCars.ItemsSource = DbHelper.GetCarsByFilters(brand, model, year).DefaultView;
        }

        private void BtnResetCars_Click(object sender, RoutedEventArgs e)
        {
            txtSearchBrand.Clear();
            txtSearchModel.Clear();
            txtSearchYear.Clear();
            LoadAllData();
        }

        private void BtnAddCar_Click(object sender, RoutedEventArgs e)
        {
            CarEditWindow win = new CarEditWindow();
            if (win.ShowDialog() == true) LoadAllData();
        }

        private void BtnEditCar_Click(object sender, RoutedEventArgs e)
        {
            if (dgCars.SelectedItem is DataRowView row)
            {
                CarEditWindow win = new CarEditWindow(row.Row);
                if (win.ShowDialog() == true) LoadAllData();
            }
            else MessageBox.Show("Выберите автомобиль для редактирования");
        }

        // ОБНОВЛЕННЫЙ МЕТОД УДАЛЕНИЯ
        private void BtnDeleteCar_Click(object sender, RoutedEventArgs e)
        {
            if (dgCars.SelectedItem is DataRowView row)
            {
                // Проверка статуса продажи
                bool isSold = Convert.ToBoolean(row["is_sold"]);

                if (isSold)
                {
                    MessageBox.Show("Проданный авто удалить нельзя!",
                                    "Ограничение удаления",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    return;
                }

                if (MessageBox.Show("Вы действительно хотите удалить этот автомобиль из базы данных?",
                                    "Подтверждение удаления",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        int id = (int)row["id"];
                        var p = new NpgsqlParameter[] { new NpgsqlParameter("p_id", id) };

                        DbHelper.ExecuteProcedure("public.delete_car", p);
                        LoadAllData();

                        MessageBox.Show("Автомобиль успешно удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при обращении к базе данных: " + ex.Message);
                    }
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, сначала выберите автомобиль в таблице.");
            }
        }

        // --- ВКЛАДКА КЛИЕНТЫ ---

        private void dgClients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgClients.SelectedItem is DataRowView row)
            {
                txtClientName.Text = row["fullname"]?.ToString() ?? "";
                txtClientPhone.Text = row["phone"]?.ToString() ?? "";
            }
        }

        private void BtnAddClient_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtClientName.Text)) { MessageBox.Show("Введите ФИО"); return; }
            try
            {
                var p = new NpgsqlParameter[] {
                    new NpgsqlParameter("p_oper", NpgsqlDbType.Text) { Value = "INSERT" },
                    new NpgsqlParameter("p_fullname", NpgsqlDbType.Varchar) { Value = txtClientName.Text },
                    new NpgsqlParameter("p_phone", NpgsqlDbType.Varchar) { Value = (object)txtClientPhone.Text ?? DBNull.Value },
                    new NpgsqlParameter("p_id", NpgsqlDbType.Integer) { Value = DBNull.Value }
                };
                DbHelper.ExecuteProcedure("public.manage_client", p);
                txtClientName.Clear(); txtClientPhone.Clear();
                LoadAllData();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void BtnEditClient_Click(object sender, RoutedEventArgs e)
        {
            if (dgClients.SelectedItem is DataRowView row)
            {
                try
                {
                    var p = new NpgsqlParameter[] {
                        new NpgsqlParameter("p_id", NpgsqlDbType.Integer) { Value = (int)row["id"] },
                        new NpgsqlParameter("p_fullname", NpgsqlDbType.Varchar) { Value = txtClientName.Text },
                        new NpgsqlParameter("p_phone", NpgsqlDbType.Varchar) { Value = (object)txtClientPhone.Text ?? DBNull.Value }
                    };
                    DbHelper.ExecuteProcedure("public.update_client", p);
                    LoadAllData();
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
        }

        private void BtnDeleteClient_Click(object sender, RoutedEventArgs e)
        {
            if (dgClients.SelectedItem is DataRowView row)
            {
                if (MessageBox.Show("Удалить клиента?", "Удаление", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    var p = new NpgsqlParameter[] {
                        new NpgsqlParameter("p_oper", NpgsqlDbType.Text) { Value = "DELETE" },
                        new NpgsqlParameter("p_fullname", DBNull.Value),
                        new NpgsqlParameter("p_phone", DBNull.Value),
                        new NpgsqlParameter("p_id", NpgsqlDbType.Integer) { Value = (int)row["id"] }
                    };
                    DbHelper.ExecuteProcedure("public.manage_client", p);
                    LoadAllData();
                }
            }
        }

        // --- ВКЛАДКА ПРОДАЖИ ---

        private void RefreshSalesComboBoxes()
        {
            cbCars.ItemsSource = DbHelper.GetTable(
                "SELECT id, brand || ' ' || model || ' (' || year || ')' as display_name, price FROM cars WHERE is_sold = false"
            ).DefaultView;

            cbClients.ItemsSource = DbHelper.GetTable("SELECT id, fullname FROM clients").DefaultView;
            dpSaleDate.SelectedDate = DateTime.Now;
        }

        private void cbCars_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbCars.SelectedItem is DataRowView row)
                txtSalePrice.Text = row["price"].ToString();
        }

        private void BtnAddSale_Click(object sender, RoutedEventArgs e)
        {
            if (cbCars.SelectedValue == null || cbClients.SelectedValue == null || string.IsNullOrEmpty(txtSalePrice.Text))
            {
                MessageBox.Show("Заполните все данные по продаже!");
                return;
            }

            try
            {
                var p = new NpgsqlParameter[] {
                    new NpgsqlParameter("p_car_id", NpgsqlDbType.Integer) { Value = cbCars.SelectedValue },
                    new NpgsqlParameter("p_client_id", NpgsqlDbType.Integer) { Value = cbClients.SelectedValue },
                    new NpgsqlParameter("p_price_sold", NpgsqlDbType.Numeric) { Value = decimal.Parse(txtSalePrice.Text) },
                    new NpgsqlParameter("p_sale_date", NpgsqlDbType.Date) { Value = dpSaleDate.SelectedDate ?? DateTime.Now }
                };
                DbHelper.ExecuteProcedure("public.register_sale", p);
                MessageBox.Show("Успешно продано!");
                LoadAllData();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void BtnDeleteSale_Click(object sender, RoutedEventArgs e)
        {
            if (dgSales.SelectedItem is DataRowView row)
            {
                if (MessageBox.Show("Отменить продажу? Авто станет снова доступным.", "Внимание", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try
                    {
                        int saleId = (int)row["sale_id"];
                        DbHelper.ExecuteNonQuery($"UPDATE cars SET is_sold = false WHERE id = (SELECT car_id FROM sales WHERE id = {saleId})");
                        DbHelper.ExecuteNonQuery($"DELETE FROM sales WHERE id = {saleId}");
                        LoadAllData();
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message); }
                }
            }
        }

        private void TxtSearchSaleClient_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filter = txtSearchSaleClient.Text.Trim();
            string sql = "SELECT * FROM public.get_sales_with_details()";
            if (!string.IsNullOrEmpty(filter))
                sql += $" WHERE client_fullname ILIKE '%{filter}%'";

            dgSales.ItemsSource = DbHelper.GetTable(sql).DefaultView;
        }

        private void dgSales_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        // --- ВКЛАДКА ПОЛЬЗОВАТЕЛИ ---

        private void dgUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgUsers.SelectedItem is DataRowView row)
            {
                txtUserLogin.Text = row["login"]?.ToString();
                txtUserPassword.Text = row["password"]?.ToString();
                cbUserRole.SelectedValue = row["role_id"];
            }
        }

        private void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUserLogin.Text) || string.IsNullOrWhiteSpace(txtUserPassword.Text) || cbUserRole.SelectedValue == null)
            {
                MessageBox.Show("Заполните все поля!");
                return;
            }

            try
            {
                string sql = "INSERT INTO users (login, password, role_id) VALUES (@login, @pass, @role)";
                var p = new NpgsqlParameter[] {
                    new NpgsqlParameter("@login", txtUserLogin.Text),
                    new NpgsqlParameter("@pass", txtUserPassword.Text),
                    new NpgsqlParameter("@role", (int)cbUserRole.SelectedValue)
                };
                DbHelper.ExecuteNonQuery(sql, p);
                LoadAllData();
                ClearUserFields();
            }
            catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
        }

        private void BtnEditUser_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsers.SelectedItem is DataRowView row)
            {
                try
                {
                    string sql = "UPDATE users SET login = @login, password = @pass, role_id = @role WHERE id = @id";
                    var p = new NpgsqlParameter[] {
                        new NpgsqlParameter("@login", txtUserLogin.Text),
                        new NpgsqlParameter("@pass", txtUserPassword.Text),
                        new NpgsqlParameter("@role", (int)cbUserRole.SelectedValue),
                        new NpgsqlParameter("@id", (int)row["id"])
                    };
                    DbHelper.ExecuteNonQuery(sql, p);
                    LoadAllData();
                }
                catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
            }
        }

        private void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsers.SelectedItem is DataRowView row)
            {
                int idToDelete = (int)row["id"];
                if (idToDelete == _userId)
                {
                    MessageBox.Show("Нельзя удалить себя!");
                    return;
                }

                if (MessageBox.Show("Удалить пользователя?", "Внимание", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    DbHelper.ExecuteNonQuery($"DELETE FROM users WHERE id = {idToDelete}");
                    LoadAllData();
                    ClearUserFields();
                }
            }
        }

        private void ClearUserFields()
        {
            txtUserLogin.Clear();
            txtUserPassword.Clear();
            cbUserRole.SelectedIndex = -1;
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWin = new LoginWindow();
            loginWin.Show();
            this.Close();
        }
    }
}