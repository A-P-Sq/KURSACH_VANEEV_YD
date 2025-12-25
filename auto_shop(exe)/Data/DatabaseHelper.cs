using Npgsql;
using System;
using System.Data;

namespace AutoShopCoursework
{
    public static class DbHelper
    {
        // 1. Метод для получения актуальной строки подключения из настроек приложения
        private static string GetConnectionString()
        {
            // Если ты заполнил таблицу в Шаге 1, это заработает:
            if (string.IsNullOrEmpty(auto_shop.Properties.Settings.Default.ConnectionString))
            {
                return "Host=localhost;Port=5433;Username=postgres;Password=SA;Database=auto_shop";
            }
            return auto_shop.Properties.Settings.Default.ConnectionString;
        }

        // 2. Вспомогательный метод для создания подключения
        private static NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(GetConnectionString());
        }

        // 3. Метод для получения данных (SELECT)
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

        // 4. Метод для выполнения прямых SQL-команд (UPDATE, DELETE, INSERT)
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

        // 5. Метод для выполнения хранимых процедур
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

        // 6. Специальный метод для функции фильтрации автомобилей
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