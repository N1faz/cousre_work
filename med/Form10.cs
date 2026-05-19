using System;
using System.Data;
using System.Data.SQLite;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace med
{
    public partial class Form10 : Form
    {
        public Form10()
        {
            InitializeComponent();
        }

        // Кнопка "Добавить вакцинацию"
        private void button4_Click(object sender, EventArgs e)
        {
            // Проверка подключения к базе данных
            if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
            {
                MessageBox.Show("Подключение к базе данных не установлено!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // ============= 1. ПРОВЕРКА ДАТЫ (textBox1) =============
            string vaccinationDate = textBox1.Text.Trim();
            if (string.IsNullOrWhiteSpace(vaccinationDate))
            {
                MessageBox.Show("Введите дату вакцинации!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox1.Focus();
                return;
            }

            // Проверка формата даты (ГГГГ-ММ-ДД)
            string datePattern = @"^\d{4}-\d{2}-\d{2}$";
            if (!Regex.IsMatch(vaccinationDate, datePattern))
            {
                MessageBox.Show("Дата вакцинации должна быть в формате ГГГГ-ММ-ДД!\n" +
                    "Пример: 2024-01-15", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox1.Focus();
                return;
            }

            DateTime parsedDate;
            if (!DateTime.TryParseExact(vaccinationDate, "yyyy-MM-dd", null,
                System.Globalization.DateTimeStyles.None, out parsedDate))
            {
                MessageBox.Show("Введите корректную дату!\nПример: 2024-01-15", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox1.Focus();
                return;
            }

            // ============= 2. ПРИМЕЧАНИЯ (textBox2) - необязательное поле =============
            string notes = textBox2.Text.Trim();
            if (string.IsNullOrWhiteSpace(notes))
            {
                notes = null;
            }

            // ============= 3. ПОИСК ЖИВОТНОГО ПО КЛИЧКЕ (textBox3) =============
            string animalName = textBox3.Text.Trim();
            if (string.IsNullOrWhiteSpace(animalName))
            {
                MessageBox.Show("Введите кличку животного!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox3.Focus();
                return;
            }

            // Получаем animal_id по кличке
            int animalId = -1;
            string sqlAnimal = "SELECT animal_id FROM animals WHERE name = @name";
            using (var cmd = new SQLiteCommand(sqlAnimal, Program.DatabaseConnection))
            {
                cmd.Parameters.AddWithValue("@name", animalName);
                var result = cmd.ExecuteScalar();
                if (result == null)
                {
                    MessageBox.Show($"Животное с кличкой '{animalName}' не найдено в базе данных!\n" +
                        "Проверьте правильность написания или сначала добавьте животное.",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox3.Focus();
                    textBox3.SelectAll();
                    return;
                }
                animalId = Convert.ToInt32(result);
            }

            // ============= 4. ПОИСК ВРАЧА (textBox6 - фамилия, textBox4 - имя, textBox7 - отчество) =============
            string doctorLastName = textBox6.Text.Trim();   // Фамилия
            string doctorFirstName = textBox4.Text.Trim();  // Имя
            string doctorMiddleName = textBox7.Text.Trim(); // Отчество

            if (string.IsNullOrWhiteSpace(doctorLastName))
            {
                MessageBox.Show("Введите фамилию врача!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox6.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(doctorFirstName))
            {
                MessageBox.Show("Введите имя врача!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox4.Focus();
                return;
            }

            // Если отчество не введено, устанавливаем null
            if (string.IsNullOrWhiteSpace(doctorMiddleName))
            {
                doctorMiddleName = null;
            }

            // Проверяем существование врача
            int doctorId = -1;
            string sqlDoctor = @"
                SELECT doctor_id FROM doctors 
                WHERE last_name = @last_name 
                  AND first_name = @first_name 
                  AND (middle_name = @middle_name OR (middle_name IS NULL AND @middle_name IS NULL))";

            using (var cmd = new SQLiteCommand(sqlDoctor, Program.DatabaseConnection))
            {
                cmd.Parameters.AddWithValue("@last_name", doctorLastName);
                cmd.Parameters.AddWithValue("@first_name", doctorFirstName);
                cmd.Parameters.AddWithValue("@middle_name", doctorMiddleName);
                var result = cmd.ExecuteScalar();
                if (result == null)
                {
                    string doctorFullName = $"{doctorLastName} {doctorFirstName} {(string.IsNullOrEmpty(doctorMiddleName) ? "" : doctorMiddleName)}".Trim();
                    MessageBox.Show($"Врач '{doctorFullName}' не найден в базе данных!\n\n" +
                        "Проверьте правильность ввода ФИО.",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox6.Focus();
                    return;
                }
                doctorId = Convert.ToInt32(result);
            }

            // ============= 5. ПОИСК ИЛИ СОЗДАНИЕ ВАКЦИНЫ (textBox5) =============
            string vaccineName = textBox5.Text.Trim();
            if (string.IsNullOrWhiteSpace(vaccineName))
            {
                MessageBox.Show("Введите название вакцины!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox5.Focus();
                return;
            }

            // Получаем passport_id животного (нужен для создания вакцины)
            int passportId = -1;
            string sqlPassport = "SELECT passport_id FROM vet_passports WHERE animal_id = @animal_id";
            using (var cmd = new SQLiteCommand(sqlPassport, Program.DatabaseConnection))
            {
                cmd.Parameters.AddWithValue("@animal_id", animalId);
                var result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    passportId = Convert.ToInt32(result);
                }
            }

            // Получаем vaccine_id по названию или создаём новую вакцину
            int vaccineId = -1;
            string sqlVaccine = "SELECT vaccine_id FROM vaccines WHERE name = @name";
            using (var cmd = new SQLiteCommand(sqlVaccine, Program.DatabaseConnection))
            {
                cmd.Parameters.AddWithValue("@name", vaccineName);
                var result = cmd.ExecuteScalar();
                if (result != null)
                {
                    vaccineId = Convert.ToInt32(result);
                }
                else
                {
                    // Вакцина не найдена - создаём новую
                    if (passportId == -1)
                    {
                        MessageBox.Show($"У животного '{animalName}' нет ветеринарного паспорта!\n" +
                            "Сначала добавьте ветеринарный паспорт для этого животного.",
                            "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    DialogResult createVaccine = MessageBox.Show($"Вакцина '{vaccineName}' не найдена в базе данных.\n\n" +
                        "Хотите создать новую вакцину с этим названием?",
                        "Создание новой вакцины",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (createVaccine == DialogResult.Yes)
                    {
                        string insertVaccine = @"
                            INSERT INTO vaccines (name, manufacturer, expiry_date, price, purpose, passport_id)
                            VALUES (@name, @manufacturer, @expiry_date, @price, @purpose, @passport_id);
                            SELECT last_insert_rowid();";

                        using (var cmdInsert = new SQLiteCommand(insertVaccine, Program.DatabaseConnection))
                        {
                            cmdInsert.Parameters.AddWithValue("@name", vaccineName);
                            cmdInsert.Parameters.AddWithValue("@manufacturer", "Не указан");
                            cmdInsert.Parameters.AddWithValue("@expiry_date", DateTime.Now.AddYears(1).ToString("yyyy-MM-dd"));
                            cmdInsert.Parameters.AddWithValue("@price", 0);
                            cmdInsert.Parameters.AddWithValue("@purpose", "Не указано");
                            cmdInsert.Parameters.AddWithValue("@passport_id", passportId);
                            vaccineId = Convert.ToInt32(cmdInsert.ExecuteScalar());
                        }

                        MessageBox.Show($"✓ Вакцина '{vaccineName}' успешно создана!",
                            "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        textBox5.Focus();
                        textBox5.SelectAll();
                        return;
                    }
                }
            }

            // ============= 6. ДОБАВЛЕНИЕ ВАКЦИНАЦИИ =============
            try
            {
                // Триггер trigger_vaccination_status_insert автоматически установит статус
                string insertSql = @"
                    INSERT INTO vaccinations (vaccination_date, notes, animal_id, vaccine_id, doctor_id)
                    VALUES (@vaccination_date, @notes, @animal_id, @vaccine_id, @doctor_id);
                    SELECT last_insert_rowid();";

                long newVaccinationId;
                using (var cmd = new SQLiteCommand(insertSql, Program.DatabaseConnection))
                {
                    cmd.Parameters.AddWithValue("@vaccination_date", vaccinationDate);
                    cmd.Parameters.AddWithValue("@notes", notes);
                    cmd.Parameters.AddWithValue("@animal_id", animalId);
                    cmd.Parameters.AddWithValue("@vaccine_id", vaccineId);
                    cmd.Parameters.AddWithValue("@doctor_id", doctorId);
                    newVaccinationId = (long)cmd.ExecuteScalar();
                }

                // Получаем статус, который установил триггер
                string status = "";
                string getStatusSql = "SELECT status FROM vaccinations WHERE vaccination_id = @id";
                using (var cmd = new SQLiteCommand(getStatusSql, Program.DatabaseConnection))
                {
                    cmd.Parameters.AddWithValue("@id", newVaccinationId);
                    var result = cmd.ExecuteScalar();
                    if (result != null) status = result.ToString();
                }

                string statusText = status == "completed" ? "Завершена" : "Запланирована";
                string statusMessage = status == "completed" ?
                    "Вакцинация завершена (дата уже прошла)" :
                    "Вакцинация запланирована (дата в будущем)";

                string doctorFullName = $"{doctorLastName} {doctorFirstName} {(string.IsNullOrEmpty(doctorMiddleName) ? "" : doctorMiddleName)}".Trim();

                MessageBox.Show($"✓ Вакцинация успешно добавлена!\n\n" +
                    $"Дата вакцинации: {vaccinationDate}\n" +
                    $"Животное: {animalName}\n" +
                    $"Вакцина: {vaccineName}\n" +
                    $"Врач: {doctorFullName}\n" +
                    $"Статус: {statusText}\n" +
                    $"Примечания: {(string.IsNullOrEmpty(notes) ? "нет" : notes)}\n\n" +
                    $"[Триггер] {statusMessage}",
                    "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Очищаем поля после успешного добавления
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении вакцинации: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Очистка полей формы
        private void ClearFields()
        {
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            textBox4.Text = "";
            textBox5.Text = "";
            textBox6.Text = "";
            textBox7.Text = "";
        }

        // Кнопка "Очистить поля"
        private void buttonClear_Click(object sender, EventArgs e)
        {
            ClearFields();
        }

        // Кнопка "Назад"
        private void buttonBack_Click(object sender, EventArgs e)
        {
            Form4 fourthForm = new Form4();
            this.Hide();
            fourthForm.Show();
        }

        // Проверка существования врача (кнопка CheckDoctor)
        private void buttonCheckDoctor_Click(object sender, EventArgs e)
        {
            string doctorLastName = textBox6.Text.Trim();
            string doctorFirstName = textBox4.Text.Trim();
            string doctorMiddleName = textBox7.Text.Trim();

            if (string.IsNullOrWhiteSpace(doctorLastName) || string.IsNullOrWhiteSpace(doctorFirstName))
            {
                MessageBox.Show("Введите фамилию и имя врача для проверки!", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(doctorMiddleName))
            {
                doctorMiddleName = null;
            }

            string sql = @"
                SELECT doctor_id, last_name, first_name, middle_name, specialization, experience_years 
                FROM doctors 
                WHERE last_name = @last_name AND first_name = @first_name 
                  AND (middle_name = @middle_name OR (middle_name IS NULL AND @middle_name IS NULL))";

            using (var cmd = new SQLiteCommand(sql, Program.DatabaseConnection))
            {
                cmd.Parameters.AddWithValue("@last_name", doctorLastName);
                cmd.Parameters.AddWithValue("@first_name", doctorFirstName);
                cmd.Parameters.AddWithValue("@middle_name", doctorMiddleName);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string specialization = reader["specialization"]?.ToString() ?? "не указана";
                        int experience = Convert.ToInt32(reader["experience_years"]);
                        string middleName = reader["middle_name"]?.ToString() ?? "";
                        string fullName = $"{doctorLastName} {doctorFirstName} {middleName}".Trim();

                        MessageBox.Show($"✓ Врач найден!\n\n" +
                            $"ФИО: {fullName}\n" +
                            $"Специализация: {specialization}\n" +
                            $"Стаж: {experience} лет",
                            "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        string fullName = $"{doctorLastName} {doctorFirstName} {(string.IsNullOrEmpty(doctorMiddleName) ? "" : doctorMiddleName)}".Trim();
                        MessageBox.Show($"Врач '{fullName}' не найден!",
                            "Информация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        // Проверка существования животного
        private void buttonCheckAnimal_Click(object sender, EventArgs e)
        {
            string animalName = textBox3.Text.Trim();
            if (string.IsNullOrWhiteSpace(animalName))
            {
                MessageBox.Show("Введите кличку животного для проверки!", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string sql = "SELECT animal_id, name, species, breed, age FROM animals WHERE name = @name";
            using (var cmd = new SQLiteCommand(sql, Program.DatabaseConnection))
            {
                cmd.Parameters.AddWithValue("@name", animalName);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        MessageBox.Show($"✓ Животное найдено!\n\n" +
                            $"Кличка: {reader["name"]}\n" +
                            $"Вид: {reader["species"]}\n" +
                            $"Порода: {reader["breed"]}\n" +
                            $"Возраст: {reader["age"]} лет",
                            "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Животное с кличкой '{animalName}' не найдено!",
                            "Информация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        // Проверка существования вакцины (обновлена с учетом создания)
        private void buttonCheckVaccine_Click(object sender, EventArgs e)
        {
            string vaccineName = textBox5.Text.Trim();
            if (string.IsNullOrWhiteSpace(vaccineName))
            {
                MessageBox.Show("Введите название вакцины для проверки!", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string sql = "SELECT vaccine_id, name, manufacturer, purpose FROM vaccines WHERE name = @name";
            using (var cmd = new SQLiteCommand(sql, Program.DatabaseConnection))
            {
                cmd.Parameters.AddWithValue("@name", vaccineName);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        MessageBox.Show($"✓ Вакцина найдена!\n\n" +
                            $"Название: {reader["name"]}\n" +
                            $"Производитель: {reader["manufacturer"]}\n" +
                            $"Назначение: {reader["purpose"]}",
                            "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Вакцина с названием '{vaccineName}' не найдена.\n\n" +
                            "При добавлении вакцинации вы сможете создать новую вакцину автоматически.",
                            "Информация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }
    }
}