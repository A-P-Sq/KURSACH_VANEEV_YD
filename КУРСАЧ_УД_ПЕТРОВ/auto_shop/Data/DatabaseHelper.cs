using Npgsql;
using System;
using System.Data;

namespace AutoShopCoursework
{
    public static class DbHelper
    {
        // Получение строки подключения
        private static string GetConnectionString()
        {
            // Используем настройки проекта, если они пусты — берем строку по умолчанию
            if (string.IsNullOrEmpty(auto_shop.Properties.Settings.Default.ConnectionString))
            {
                // ВНИМАНИЕ: Проверьте ваш порт (5432 или 5433)
                return "Host=localhost;Port=5433;Username=postgres;Password=SA;Database=auto_shop";
            }
            return auto_shop.Properties.Settings.Default.ConnectionString;
        }

        private static NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(GetConnectionString());
        }

        // Выполнение SELECT запросов
        public static DataTable GetTable(string query, NpgsqlParameter[]? parameters = null)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    if (parameters != null) cmd.Parameters.AddRange(parameters);
                    var da = new NpgsqlDataAdapter(cmd);
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        // Выполнение команд INSERT, UPDATE, DELETE
        public static void ExecuteNonQuery(string sql, NpgsqlParameter[]? parameters = null)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    if (parameters != null) cmd.Parameters.AddRange(parameters);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Вызов хранимых процедур
        public static void ExecuteProcedure(string procName, NpgsqlParameter[] parameters)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(procName, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddRange(parameters);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Фильтрация автомобилей (вызов функции БД)
        public static DataTable GetCarsByFilters(string? brand, string? model, int? year)
        {
            string sql = "SELECT * FROM public.get_cars_by_filters(@p_brand, @p_model, @p_year)";
            var parameters = new NpgsqlParameter[]
            {
                new NpgsqlParameter("p_brand", (object?)brand ?? DBNull.Value),
                new NpgsqlParameter("p_model", (object?)model ?? DBNull.Value),
                new NpgsqlParameter("p_year", (object?)year ?? DBNull.Value)
            };
            return GetTable(sql, parameters);
        }
    }
}