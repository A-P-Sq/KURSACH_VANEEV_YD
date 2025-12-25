using Npgsql;
using System;
using System.Data;
using System.Windows;

namespace AutoShopCoursework
{
    public partial class CarEditWindow : Window
    {
        private int? _carId = null;

        // Конструктор 1: Для добавления (без параметров)
        public CarEditWindow()
        {
            InitializeComponent();
        }

        // Конструктор 2: Для редактирования (с параметром DataRow)
        public CarEditWindow(DataRow row) : this()
        {
            _carId = Convert.ToInt32(row["id"]);
            txtBrand.Text = row["brand"].ToString();
            txtModel.Text = row["model"].ToString();
            txtYear.Text = row["year"].ToString();
            txtPrice.Text = row["price"].ToString();
            this.Title = "Редактирование автомобиля";
        }

        // Метод сохранения (только ОДИН раз)
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_carId == null) // INSERT
                {
                    var p = new NpgsqlParameter[] {
                        new NpgsqlParameter("p_brand", txtBrand.Text),
                        new NpgsqlParameter("p_model", txtModel.Text),
                        new NpgsqlParameter("p_year", int.Parse(txtYear.Text)),
                        new NpgsqlParameter("p_price", decimal.Parse(txtPrice.Text))
                    };
                    DbHelper.ExecuteProcedure("public.insert_car", p);
                }
                else // UPDATE
                {
                    var p = new NpgsqlParameter[] {
                        new NpgsqlParameter("p_id", _carId),
                        new NpgsqlParameter("p_brand", txtBrand.Text),
                        new NpgsqlParameter("p_model", txtModel.Text),
                        new NpgsqlParameter("p_year", int.Parse(txtYear.Text)),
                        new NpgsqlParameter("p_price", decimal.Parse(txtPrice.Text))
                    };
                    DbHelper.ExecuteProcedure("public.update_car", p);
                }
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }
    }
}