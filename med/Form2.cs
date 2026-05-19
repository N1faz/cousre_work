using System;
using System.Data;
using System.Data.SQLite;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace med
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
            this.Load += Form2_Load;
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            // Проверка роли при загрузке формы
            if (Program.CurrentUserRole != "admin")
            {
                MessageBox.Show("У вас нет прав для добавления животных!\n" +
                    "Эта функция доступна только администраторам.",
                    "Доступ запрещен",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Close();
                Form4 fourthForm = new Form4();
                fourthForm.Show();
                return;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Form4 fourthForm = new Form4();
            this.Hide();
            fourthForm.Show();
        }

        // Кнопка "Добавить животное"
        private void button4_Click(object sender, EventArgs e)
        {
            // Дополнительная проверка роли перед добавлением
            if (Program.CurrentUserRole != "admin")
            {
                MessageBox.Show("У вас нет прав для добавления животных!", "Доступ запрещен",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Close();
                Form4 fourthForm = new Form4();
                fourthForm.Show();
                return;
            }

            // Проверка подключения
            if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
            {
                MessageBox.Show("Подключение к базе данных не установлено!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // ============= ПРОВЕРКА ОБЯЗАТЕЛЬНЫХ ПОЛЕЙ =============
            // Кличка (textBox1)
            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                MessageBox.Show("Введите кличку животного!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox1.Focus();
                return;
            }

            // Фамилия владельца (textBox10)
            if (string.IsNullOrWhiteSpace(textBox10.Text))
            {
                MessageBox.Show("Введите фамилию владельца!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox10.Focus();
                return;
            }

            // Имя владельца (textBox4)
            if (string.IsNullOrWhiteSpace(textBox4.Text))
            {
                MessageBox.Show("Введите имя владельца!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox4.Focus();
                return;
            }

            // Телефон (textBox5)
            if (string.IsNullOrWhiteSpace(textBox5.Text))
            {
                MessageBox.Show("Введите контактный телефон владельца!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox5.Focus();
                return;
            }

            // ============= ПРОВЕРКА ЛОГИНА ПОЛЬЗОВАТЕЛЯ (ОБЯЗАТЕЛЬНО) =============
            string username = textBox12.Text.Trim();

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Введите логин пользователя!\n" +
                    "Только зарегистрированные пользователи могут быть владельцами животных.",
                    "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox12.Focus();
                return;
            }

            // Проверка существования пользователя по логину
            int ownerIdFromUser = -1;
            string checkUserExists = @"
                SELECT u.user_id, u.owner_id 
                FROM users u 
                WHERE u.username = @username";

            using (var cmd = new SQLiteCommand(checkUserExists, Program.DatabaseConnection))
            {
                cmd.Parameters.AddWithValue("@username", username);
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        MessageBox.Show($"Пользователь с логином '{username}' не найден в системе!\n\n" +
                            "Пожалуйста, убедитесь, что:\n" +
                            "1. Пользователь зарегистрирован в системе\n" +
                            "2. Логин введён правильно\n\n" +
                            "Если пользователь не существует, сначала создайте его через административную панель.",
                            "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        textBox12.Focus();
                        textBox12.SelectAll();
                        return;
                    }

                    // Проверяем, привязан ли уже пользователь к владельцу
                    ownerIdFromUser = reader["owner_id"] != DBNull.Value ? Convert.ToInt32(reader["owner_id"]) : -1;
                }
            }

            // ============= ПРОВЕРКА ВОЗРАСТА (textBox3) =============
            int age = 0;
            if (!string.IsNullOrWhiteSpace(textBox3.Text))
            {
                if (!int.TryParse(textBox3.Text, out age))
                {
                    MessageBox.Show("Возраст должен быть целым числом!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox3.Focus();
                    return;
                }
                if (age < 0 || age > 50)
                {
                    MessageBox.Show("Возраст должен быть от 0 до 50 лет!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox3.Focus();
                    return;
                }
            }

            // ============= ПРОВЕРКА ДАТЫ (textBox9) - ФОРМАТ ГГГГ-ММ-ДД =============
            if (!string.IsNullOrWhiteSpace(textBox9.Text))
            {
                string dateString = textBox9.Text.Trim();

                string pattern = @"^\d{4}-\d{2}-\d{2}$";
                if (!Regex.IsMatch(dateString, pattern))
                {
                    MessageBox.Show("Дата выдачи ветпаспорта должна быть в формате ГГГГ-ММ-ДД!\n" +
                        "Пример: 2024-01-15", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox9.Focus();
                    return;
                }

                DateTime tempDate;
                if (!DateTime.TryParseExact(dateString, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out tempDate))
                {
                    MessageBox.Show("Введите корректную дату!\n" +
                        "Пример правильной даты: 2024-01-15", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox9.Focus();
                    return;
                }

                if (tempDate > DateTime.Now)
                {
                    DialogResult result = MessageBox.Show("Дата выдачи паспорта указана в будущем.\n" +
                        "Вы уверены, что хотите продолжить?", "Предупреждение",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.No)
                    {
                        textBox9.Focus();
                        return;
                    }
                }
            }

            // ============= ПРОВЕРКА ТЕЛЕФОНА =============
            string phone = textBox5.Text.Trim();
            if (phone.Length < 10)
            {
                MessageBox.Show("Введите корректный номер телефона (минимум 10 цифр)!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox5.Focus();
                return;
            }

            try
            {
                // Получаем данные владельца (раздельные поля)
                string lastName = textBox10.Text.Trim();
                string firstName = textBox4.Text.Trim();
                string middleName = string.IsNullOrWhiteSpace(textBox11.Text) ? null : textBox11.Text.Trim();

                int ownerId;

                // ============= 1. ОБРАБОТКА ВЛАДЕЛЬЦА =============
                // Если пользователь уже привязан к владельцу, используем существующего владельца
                if (ownerIdFromUser > 0)
                {
                    ownerId = ownerIdFromUser;

                    // Обновляем данные владельца (ФИО и телефон), если они изменились
                    string updateOwner = @"
                        UPDATE owners 
                        SET last_name = @last_name, 
                            first_name = @first_name, 
                            middle_name = @middle_name,
                            phone = @phone
                        WHERE owner_id = @owner_id";

                    using (var cmd = new SQLiteCommand(updateOwner, Program.DatabaseConnection))
                    {
                        cmd.Parameters.AddWithValue("@last_name", lastName);
                        cmd.Parameters.AddWithValue("@first_name", firstName);
                        cmd.Parameters.AddWithValue("@middle_name", middleName);
                        cmd.Parameters.AddWithValue("@phone", phone);
                        cmd.Parameters.AddWithValue("@owner_id", ownerId);
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    // Создаём нового владельца
                    ownerId = CreateNewOwner(lastName, firstName, middleName, phone);
                    if (ownerId == -1)
                    {
                        MessageBox.Show("Не удалось добавить владельца!", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Привязываем пользователя к созданному владельцу
                    string linkUserToOwner = "UPDATE users SET owner_id = @owner_id WHERE username = @username";
                    using (var cmd = new SQLiteCommand(linkUserToOwner, Program.DatabaseConnection))
                    {
                        cmd.Parameters.AddWithValue("@owner_id", ownerId);
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.ExecuteNonQuery();
                    }
                }

                // ============= 2. ДОБАВЛЕНИЕ ЖИВОТНОГО =============
                string animalName = textBox1.Text.Trim();
                string breed = string.IsNullOrWhiteSpace(textBox2.Text) ? "Не указана" : textBox2.Text.Trim();
                string species = string.IsNullOrWhiteSpace(textBox6.Text) ? "Не указан" : textBox6.Text.Trim();

                string insertAnimal = @"
                    INSERT INTO animals (name, age, breed, species, owner_id)
                    VALUES (@name, @age, @breed, @species, @owner_id);
                    SELECT last_insert_rowid();";

                int animalId;
                using (var cmd = new SQLiteCommand(insertAnimal, Program.DatabaseConnection))
                {
                    cmd.Parameters.AddWithValue("@name", animalName);
                    cmd.Parameters.AddWithValue("@age", age);
                    cmd.Parameters.AddWithValue("@breed", breed);
                    cmd.Parameters.AddWithValue("@species", species);
                    cmd.Parameters.AddWithValue("@owner_id", ownerId);
                    animalId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                if (animalId == 0)
                {
                    MessageBox.Show("Не удалось добавить животное!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // ============= 3. ДОБАВЛЕНИЕ ВЕТПАСПОРТА =============
                if (!string.IsNullOrWhiteSpace(textBox9.Text))
                {
                    string country = string.IsNullOrWhiteSpace(textBox8.Text) ? "Россия" : textBox8.Text.Trim();

                    string insertPassport = @"
                        INSERT INTO vet_passports (issue_date, country_of_issue, owner_id, animal_id)
                        VALUES (@issue_date, @country, @owner_id, @animal_id)";

                    using (var cmd = new SQLiteCommand(insertPassport, Program.DatabaseConnection))
                    {
                        cmd.Parameters.AddWithValue("@issue_date", textBox9.Text.Trim());
                        cmd.Parameters.AddWithValue("@country", country);
                        cmd.Parameters.AddWithValue("@owner_id", ownerId);
                        cmd.Parameters.AddWithValue("@animal_id", animalId);
                        cmd.ExecuteNonQuery();
                    }
                }

                // ============= 4. ДОБАВЛЕНИЕ ВАКЦИНЫ =============
                if (!string.IsNullOrWhiteSpace(textBox7.Text))
                {
                    string getPassportId = "SELECT passport_id FROM vet_passports WHERE animal_id = @animal_id";
                    int passportId = -1;
                    using (var cmd = new SQLiteCommand(getPassportId, Program.DatabaseConnection))
                    {
                        cmd.Parameters.AddWithValue("@animal_id", animalId);
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            passportId = Convert.ToInt32(result);
                        }
                    }

                    if (passportId > 0)
                    {
                        string purpose = string.IsNullOrWhiteSpace(textBox6.Text) ? "Не указано" : textBox6.Text.Trim();

                        string insertVaccine = @"
                            INSERT INTO vaccines (name, manufacturer, expiry_date, price, purpose, passport_id)
                            VALUES (@name, @manufacturer, @expiry_date, @price, @purpose, @passport_id)";

                        using (var cmd = new SQLiteCommand(insertVaccine, Program.DatabaseConnection))
                        {
                            cmd.Parameters.AddWithValue("@name", textBox7.Text.Trim());
                            cmd.Parameters.AddWithValue("@manufacturer", "Не указан");
                            cmd.Parameters.AddWithValue("@expiry_date", "2025-12-31");
                            cmd.Parameters.AddWithValue("@price", 0);
                            cmd.Parameters.AddWithValue("@purpose", purpose);
                            cmd.Parameters.AddWithValue("@passport_id", passportId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                // Формируем ФИО для сообщения
                string fullName = $"{lastName} {firstName} {(string.IsNullOrEmpty(middleName) ? "" : middleName)}".Trim();

                // Сообщение об успехе
                MessageBox.Show($"✓ Животное успешно добавлено!\n\n" +
                    $"Кличка: {animalName}\n" +
                    $"Возраст: {age}\n" +
                    $"Порода: {breed}\n" +
                    $"Вид: {species}\n" +
                    $"Владелец: {fullName}\n" +
                    $"Телефон: {phone}\n" +
                    $"Логин пользователя: {username}",
                    "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Очищаем поля
                ClearFields();

                // Обновляем Form4
                Form4 form4 = Application.OpenForms["Form4"] as Form4;
                if (form4 != null)
                {
                    form4.RefreshData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Метод для создания нового владельца
        private int CreateNewOwner(string lastName, string firstName, string middleName, string phone)
        {
            string insertOwner = @"
                INSERT INTO owners (last_name, first_name, middle_name, phone)
                VALUES (@last_name, @first_name, @middle_name, @phone);
                SELECT last_insert_rowid();";

            using (var cmd = new SQLiteCommand(insertOwner, Program.DatabaseConnection))
            {
                cmd.Parameters.AddWithValue("@last_name", lastName);
                cmd.Parameters.AddWithValue("@first_name", firstName);
                cmd.Parameters.AddWithValue("@middle_name", middleName);
                cmd.Parameters.AddWithValue("@phone", phone);
                return Convert.ToInt32(cmd.ExecuteScalar());
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
            textBox7.Text = "";
            textBox8.Text = "";
            textBox9.Text = "";
            textBox10.Text = "";
            textBox11.Text = "";
            textBox12.Text = "";
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}