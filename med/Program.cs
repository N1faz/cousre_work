using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;

namespace med
{
    internal static class Program
    {
        // Глобальное подключение к БД
        public static SQLiteConnection DatabaseConnection { get; private set; }
        public static string CurrentUserRole { get; set; }
        [STAThread]
        static void Main()
        {
            // Указываем полный путь к файлу БД
            string dbPath = "vet_clinic.db";
            string connectionString = $"Data Source={dbPath};Version=3;";

            DatabaseConnection = new SQLiteConnection(connectionString);

            try
            {
                DatabaseConnection.Open();
                MessageBox.Show("Подключение к БД успешно!", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к БД: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form8());

            // Закрываем подключение при выходе
            if (DatabaseConnection != null && DatabaseConnection.State == System.Data.ConnectionState.Open)
            {
                DatabaseConnection.Close();
                DatabaseConnection.Dispose();
            }
        }
    }
}