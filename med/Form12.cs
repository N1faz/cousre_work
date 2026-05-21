using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace med
{
    public partial class Form12 : Form
    {
        public Form12()
        {
            InitializeComponent();
            this.Load += Form12_Load;
        }

        private void Form12_Load(object sender, EventArgs e)
        {
            // Загружаем список животных в comboBox3
            LoadAnimalsToComboBox();
            // Загружаем список врачей в comboBox2
            LoadDoctorsToComboBox();
        }

        // Загрузка списка животных в comboBox3
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
                    comboBox3.DataSource = dt;
                    comboBox3.DisplayMember = "name";
                    comboBox3.ValueMember = "animal_id";
                    comboBox3.SelectedIndex = -1;
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

        // Кнопка "Добавить прием"
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
            string appointmentDate = textBox1.Text.Trim();
            if (string.IsNullOrWhiteSpace(appointmentDate))
            {
                MessageBox.Show("Введите дату приема!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox1.Focus();
                return;
            }

            // Проверка формата даты (ГГГГ-ММ-ДД)
            string datePattern = @"^\d{4}-\d{2}-\d{2}$";
            if (!Regex.IsMatch(appointmentDate, datePattern))
            {
                MessageBox.Show("Дата приема должна быть в формате ГГГГ-ММ-ДД!\n" +
                    "Пример: 2024-01-15", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox1.Focus();
                return;
            }

            DateTime parsedDate;
            if (!DateTime.TryParseExact(appointmentDate, "yyyy-MM-dd", null,
                System.Globalization.DateTimeStyles.None, out parsedDate))
            {
                MessageBox.Show("Введите корректную дату приема!\nПример: 2024-01-15", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox1.Focus();
                return;
            }

            // ============= 2. ДИАГНОЗ (textBox2) - необязательное поле =============
            string diagnosis = textBox2.Text.Trim();
            if (string.IsNullOrWhiteSpace(diagnosis))
            {
                diagnosis = null;
            }

            // ============= 3. ПРОВЕРКА ВЫБОРА ЖИВОТНОГО (comboBox3) =============
            if (comboBox3.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите животное из списка!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                comboBox3.Focus();
                return;
            }

            int animalId = Convert.ToInt32(comboBox3.SelectedValue);

            // ============= 4. ПРОВЕРКА ВЫБОРА ВРАЧА (comboBox2) =============
            if (comboBox2.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите врача из списка!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                comboBox2.Focus();
                return;
            }

            int doctorId = Convert.ToInt32(comboBox2.SelectedValue);

            // ============= 5. ДОБАВЛЕНИЕ ПРИЕМА =============
            try
            {
                string insertSql = @"
                    INSERT INTO appointments (appointment_date, diagnosis, animal_id, doctor_id)
                    VALUES (@appointment_date, @diagnosis, @animal_id, @doctor_id);
                    SELECT last_insert_rowid();";

                long newAppointmentId;
                using (var cmd = new SQLiteCommand(insertSql, Program.DatabaseConnection))
                {
                    cmd.Parameters.AddWithValue("@appointment_date", appointmentDate);
                    cmd.Parameters.AddWithValue("@diagnosis", diagnosis);
                    cmd.Parameters.AddWithValue("@animal_id", animalId);
                    cmd.Parameters.AddWithValue("@doctor_id", doctorId);
                    newAppointmentId = (long)cmd.ExecuteScalar();
                }

                // Получаем названия для сообщения
                string animalName = comboBox3.Text;
                string doctorName = comboBox2.Text;

                MessageBox.Show($"✓ Прием успешно добавлен!\n\n" +
                    $"Дата приема: {appointmentDate}\n" +
                    $"Животное: {animalName}\n" +
                    $"Врач: {doctorName}\n" +
                    $"Диагноз: {(string.IsNullOrEmpty(diagnosis) ? "не указан" : diagnosis)}\n" +
                    $"ID приема: {newAppointmentId}",
                    "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Переход на Form11
                Form11 form11 = new Form11();
                this.Hide();
                form11.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении приема: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Очистка полей формы
        private void ClearFields()
        {
            textBox1.Text = "";
            textBox2.Text = "";
            comboBox3.SelectedIndex = -1;
            comboBox2.SelectedIndex = -1;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Метод не используется, но нужен для конструктора
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Метод не используется, но нужен для конструктора
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Метод не используется, но нужен для конструктора
        }
    }
}