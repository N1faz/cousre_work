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
            this.Load += Form10_Load;
        }

        private void Form10_Load(object sender, EventArgs e)
        {
            // Загружаем список животных в comboBox1
            LoadAnimalsToComboBox();
            // Загружаем список врачей в comboBox2
            LoadDoctorsToComboBox();
            // Загружаем список вакцин в comboBox3
            LoadVaccinesToComboBox();
        }

        // Загрузка списка животных в comboBox1
        private void LoadAnimalsToComboBox()
        {
            try
            {
                if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
                {
                    MessageBox.Show("Подключение к базе данных не установлено!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string sql = "SELECT animal_id, name FROM animals ORDER BY name";
                using (var cmd = new SQLiteCommand(sql, Program.DatabaseConnection))
                {
                    DataTable dt = new DataTable();
                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                    comboBox1.DataSource = dt;
                    comboBox1.DisplayMember = "name";
                    comboBox1.ValueMember = "animal_id";
                    comboBox1.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки животных: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Загрузка списка врачей в comboBox2 (с ФИО)
        private void LoadDoctorsToComboBox()
        {
            try
            {
                if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
                {
                    MessageBox.Show("Подключение к базе данных не установлено!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Формируем ФИО врача из раздельных полей
                string sql = @"
                    SELECT 
                        doctor_id,
                        last_name || ' ' || first_name || ' ' || COALESCE(middle_name, '') AS full_name
                    FROM doctors
                    ORDER BY last_name";

                using (var cmd = new SQLiteCommand(sql, Program.DatabaseConnection))
                {
                    DataTable dt = new DataTable();
                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                    comboBox2.DataSource = dt;
                    comboBox2.DisplayMember = "full_name";
                    comboBox2.ValueMember = "doctor_id";
                    comboBox2.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки врачей: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Загрузка списка вакцин в comboBox3
        private void LoadVaccinesToComboBox()
        {
            try
            {
                if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
                {
                    MessageBox.Show("Подключение к базе данных не установлено!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string sql = "SELECT vaccine_id, name FROM vaccines ORDER BY name";
                using (var cmd = new SQLiteCommand(sql, Program.DatabaseConnection))
                {
                    DataTable dt = new DataTable();
                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                    comboBox3.DataSource = dt;
                    comboBox3.DisplayMember = "name";
                    comboBox3.ValueMember = "vaccine_id";
                    comboBox3.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки вакцин: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

            // ============= 3. ПРОВЕРКА ВЫБОРА ЖИВОТНОГО (comboBox1) =============
            if (comboBox1.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите животное из списка!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                comboBox1.Focus();
                return;
            }
            int animalId = Convert.ToInt32(comboBox1.SelectedValue);
            string animalName = comboBox1.Text;

            // ============= 4. ПРОВЕРКА ВЫБОРА ВРАЧА (comboBox2) =============
            if (comboBox2.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите врача из списка!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                comboBox2.Focus();
                return;
            }
            int doctorId = Convert.ToInt32(comboBox2.SelectedValue);
            string doctorName = comboBox2.Text;

            // ============= 5. ПРОВЕРКА ВЫБОРА ВАКЦИНЫ (comboBox3) =============
            if (comboBox3.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите вакцину из списка!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                comboBox3.Focus();
                return;
            }
            int vaccineId = Convert.ToInt32(comboBox3.SelectedValue);
            string vaccineName = comboBox3.Text;

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

                MessageBox.Show($"✓ Вакцинация успешно добавлена!\n\n" +
                    $"Дата вакцинации: {vaccinationDate}\n" +
                    $"Животное: {animalName}\n" +
                    $"Вакцина: {vaccineName}\n" +
                    $"Врач: {doctorName}\n" +
                    $"Статус: {statusText}\n" +
                    $"Примечания: {(string.IsNullOrEmpty(notes) ? "нет" : notes)}\n\n" +
                    $"[Триггер] {statusMessage}",
                    "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Переход на Form11 после успешного добавления
                Form11 form11 = new Form11();
                this.Hide();
                form11.Show();
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
            comboBox1.SelectedIndex = -1;
            comboBox2.SelectedIndex = -1;
            comboBox3.SelectedIndex = -1;
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
            if (comboBox2.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите врача из списка для проверки!", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string doctorName = comboBox2.Text;

            string sql = @"
                SELECT doctor_id, last_name, first_name, middle_name, specialization, experience_years 
                FROM doctors 
                WHERE last_name || ' ' || first_name || ' ' || COALESCE(middle_name, '') = @full_name";

            using (var cmd = new SQLiteCommand(sql, Program.DatabaseConnection))
            {
                cmd.Parameters.AddWithValue("@full_name", doctorName);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string specialization = reader["specialization"]?.ToString() ?? "не указана";
                        int experience = Convert.ToInt32(reader["experience_years"]);
                        string middleName = reader["middle_name"]?.ToString() ?? "";
                        string fullName = reader["last_name"] + " " + reader["first_name"] + " " + middleName;

                        MessageBox.Show($"✓ Врач найден!\n\n" +
                            $"ФИО: {fullName.Trim()}\n" +
                            $"Специализация: {specialization}\n" +
                            $"Стаж: {experience} лет",
                            "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Врач '{doctorName}' не найден!",
                            "Информация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        // Проверка существования животного
        private void buttonCheckAnimal_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите животное из списка для проверки!", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string animalName = comboBox1.Text;

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

        // Проверка существования вакцины
        private void buttonCheckVaccine_Click(object sender, EventArgs e)
        {
            if (comboBox3.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите вакцину из списка для проверки!", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string vaccineName = comboBox3.Text;

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
                        MessageBox.Show($"Вакцина с названием '{vaccineName}' не найдена!",
                            "Информация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}