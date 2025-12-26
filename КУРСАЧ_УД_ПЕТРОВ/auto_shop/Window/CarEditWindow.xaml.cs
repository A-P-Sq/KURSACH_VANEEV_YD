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

        // Метод сохранения с обработкой специфических ошибок БД
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Базовая проверка на заполнение полей (чтобы не упасть на int.Parse)
                if (string.IsNullOrWhiteSpace(txtYear.Text) || !int.TryParse(txtYear.Text, out _))
                {
                    MessageBox.Show("Введите корректный год выпуска (число)!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_carId == null) // Логика INSERT
                {
                    var p = new NpgsqlParameter[] {
                        new NpgsqlParameter("p_brand", txtBrand.Text),
                        new NpgsqlParameter("p_model", txtModel.Text),
                        new NpgsqlParameter("p_year", int.Parse(txtYear.Text)),
                        new NpgsqlParameter("p_price", decimal.Parse(txtPrice.Text))
                    };
                    DbHelper.ExecuteProcedure("public.insert_car", p);
                }
                else // Логика UPDATE
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
            catch (PostgresException ex)
            {
                // Перехват ошибки ограничения CHECK (код 23514)
                if (ex.SqlState == "23514" && ex.ConstraintName == "cars_year_check")
                {
                    MessageBox.Show("Неверный формат даты! Год должен быть в диапазоне от 1900 до текущего.",
                                    "Ошибка валидации",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                }
                else
                {
                    // Другие ошибки PostgreSQL
                    MessageBox.Show("Ошибка базы данных: " + ex.Message, "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                // Ошибки приведения типов (Parse) или иные системные сбои
                MessageBox.Show("Произошла ошибка: " + ex.Message, "Ошибка системы", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}