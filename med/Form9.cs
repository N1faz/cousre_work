using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace med
{
    public partial class Form9 : Form
    {
        public Form9()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Кнопка "Зарегистрироваться"
            RegisterUser();
        }

        private void RegisterUser()
        {
            try
            {
                // Проверка подключения к БД
                if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
                {
                    MessageBox.Show("Подключение к базе данных не установлено!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Проверка обязательных полей
                if (string.IsNullOrWhiteSpace(textBox1.Text)) // Фамилия
                {
                    MessageBox.Show("Введите фамилию!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox1.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(textBox2.Text)) // Имя
                {
                    MessageBox.Show("Введите имя!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox2.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(textBox4.Text)) // Телефон
                {
                    MessageBox.Show("Введите номер телефона!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox4.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(textBox5.Text)) // Логин
                {
                    MessageBox.Show("Введите логин!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox5.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(textBox6.Text)) // Пароль
                {
                    MessageBox.Show("Введите пароль!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox6.Focus();
                    return;
                }

                // Проверка, существует ли пользователь с таким логином
                string checkUser = "SELECT user_id FROM users WHERE username = @username";
                using (var cmd = new SQLiteCommand(checkUser, Program.DatabaseConnection))
                {
                    cmd.Parameters.AddWithValue("@username", textBox5.Text.Trim());
                    var existing = cmd.ExecuteScalar();
                    if (existing != null)
                    {
                        MessageBox.Show("Пользователь с таким логином уже существует!", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        textBox5.Focus();
                        return;
                    }
                }

                // Формируем ФИО
                string lastName = textBox1.Text.Trim();
                string firstName = textBox2.Text.Trim();
                string middleName = textBox3.Text.Trim();
                string fullName = $"{lastName} {firstName} {middleName}".Trim();
                string phone = textBox4.Text.Trim();
                string username = textBox5.Text.Trim();
                string password = textBox6.Text.Trim();

                // Проверяем, существует ли уже владелец с таким телефоном
                int ownerId = GetOrCreateOwner(fullName, phone);

                if (ownerId == -1)
                {
                    MessageBox.Show("Не удалось создать владельца!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Добавляем пользователя с ролью 'patient'
                // Порядок столбцов: owner_id, username, password, role, created_at
                string insertUser = @"
                    INSERT INTO users (owner_id, username, password, role, created_at)
                    VALUES (@owner_id, @username, @password, 'patient', datetime('now'))";

                using (var cmd = new SQLiteCommand(insertUser, Program.DatabaseConnection))
                {
                    cmd.Parameters.AddWithValue("@owner_id", ownerId);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show($"✓ Регистрация успешно завершена!\n\n" +
                            $"Логин: {username}\n" +
                            $"ФИО: {fullName}\n" +
                            $"Телефон: {phone}",
                            "Успех",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Очищаем поля
                        ClearFields();

                        // Закрываем форму регистрации и открываем форму входа
                        this.Hide();
                        Form8 loginForm = new Form8();
                        loginForm.Show();
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Не удалось зарегистрировать пользователя!", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при регистрации: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Метод для получения ID владельца или создания нового
        private int GetOrCreateOwner(string fullName, string phone)
        {
            try
            {
                // Проверяем по телефону
                string checkOwner = "SELECT owner_id FROM owners WHERE phone = @phone";
                using (var cmd = new SQLiteCommand(checkOwner, Program.DatabaseConnection))
                {
                    cmd.Parameters.AddWithValue("@phone", phone);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        return Convert.ToInt32(result);
                    }
                }

                // Проверяем по ФИО
                string checkOwnerByName = "SELECT owner_id FROM owners WHERE full_name = @full_name";
                using (var cmd = new SQLiteCommand(checkOwnerByName, Program.DatabaseConnection))
                {
                    cmd.Parameters.AddWithValue("@full_name", fullName);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        int ownerId = Convert.ToInt32(result);
                        // Обновляем телефон
                        string updatePhone = "UPDATE owners SET phone = @phone WHERE owner_id = @owner_id";
                        using (var updateCmd = new SQLiteCommand(updatePhone, Program.DatabaseConnection))
                        {
                            updateCmd.Parameters.AddWithValue("@phone", phone);
                            updateCmd.Parameters.AddWithValue("@owner_id", ownerId);
                            updateCmd.ExecuteNonQuery();
                        }
                        return ownerId;
                    }
                }

                // Создаем нового владельца
                string insertOwner = @"
                    INSERT INTO owners (full_name, phone)
                    VALUES (@full_name, @phone);
                    SELECT last_insert_rowid();";

                using (var cmd = new SQLiteCommand(insertOwner, Program.DatabaseConnection))
                {
                    cmd.Parameters.AddWithValue("@full_name", fullName);
                    cmd.Parameters.AddWithValue("@phone", phone);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании владельца: {ex.Message}");
                return -1;
            }
        }

        // Очистка полей
        private void ClearFields()
        {
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            textBox4.Text = "";
            textBox5.Text = "";
            textBox6.Text = "";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Кнопка "Назад"
            this.Close();
            Form8 loginForm = new Form8();
            loginForm.Show();
        }
    }
}