using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace med
{
    public partial class Form8 : Form
    {
        public Form8()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Login();
        }

        private void Login()
        {
            try
            {
                if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
                {
                    MessageBox.Show("Подключение к базе данных не установлено!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string username = textBox1.Text.Trim();
                string password = textBox2.Text.Trim();

                if (string.IsNullOrWhiteSpace(username))
                {
                    MessageBox.Show("Введите логин!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox1.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("Введите пароль!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox2.Focus();
                    return;
                }

                // Запрос с учетом структуры: user_id, owner_id, username, password, role, created_at
                string sql = @"
                    SELECT user_id, owner_id, username, password, role 
                    FROM users 
                    WHERE username = @username AND password = @password";

                using (var cmd = new SQLiteCommand(sql, Program.DatabaseConnection))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int userId = reader.GetInt32(0);
                            string role = reader.GetString(4);

                            // СОХРАНЯЕМ РОЛЬ В ГЛОБАЛЬНУЮ ПЕРЕМЕННУЮ
                            Program.CurrentUserRole = role;

                            // Обновляем время последнего входа
                            try
                            {
                                string updateSql = "UPDATE users SET last_login = datetime('now') WHERE user_id = @userId";
                                using (var updateCmd = new SQLiteCommand(updateSql, Program.DatabaseConnection))
                                {
                                    updateCmd.Parameters.AddWithValue("@userId", userId);
                                    updateCmd.ExecuteNonQuery();
                                }
                            }
                            catch { }

                            MessageBox.Show($"Добро пожаловать, {username}!\nВаша роль: {role}", "Успех",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // Перенаправление на Form4
                            Form4 mainForm = new Form4();
                            mainForm.Show();
                            this.Hide();
                        }
                        else
                        {
                            MessageBox.Show("Неверный логин или пароль!", "Ошибка",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                            textBox1.Focus();
                            textBox1.SelectAll();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при входе: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            Form9 regForm = new Form9();
            this.Hide();
            regForm.Show();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}