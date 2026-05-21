using System;
using System.Collections.Generic;
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
    public partial class Form13 : Form
    {
        private List<int> selectedDoctors = new List<int>(); // Список выбранных ID врачей
        private DataTable doctorsTable; // Таблица для хранения списка врачей

        public Form13()
        {
            InitializeComponent();
            this.Load += Form13_Load;
        }

        private void Form13_Load(object sender, EventArgs e)
        {
            // Загружаем список животных в comboBox1
            LoadAnimalsToComboBox();
            // Загружаем список врачей в comboBox2
            LoadDoctorsToComboBox();
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
                    doctorsTable = new DataTable();
                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        adapter.Fill(doctorsTable);
                    }
                    comboBox2.DataSource = doctorsTable;
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

        // Кнопка "Подтвердить выбор врачей" (button1)
        private void button1_Click(object sender, EventArgs e)
        {
            if (comboBox2.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите врача из списка для добавления!", "Предупреждение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int doctorId = Convert.ToInt32(comboBox2.SelectedValue);
            string doctorName = comboBox2.Text;

            // Проверяем, не добавлен ли уже этот врач
            if (!selectedDoctors.Contains(doctorId))
            {
                selectedDoctors.Add(doctorId);
                MessageBox.Show($"✓ Врач '{doctorName}' добавлен в список.\nВсего выбрано: {selectedDoctors.Count} врач(ей).",
                    "Выбор врача", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Врач '{doctorName}' уже добавлен в список!", "Предупреждение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Кнопка "Сбросить выбор врачей" (button2)
        private void button2_Click(object sender, EventArgs e)
        {
            selectedDoctors.Clear();
            MessageBox.Show("Список выбранных врачей очищен!", "Сброс",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Кнопка "Добавить операцию" (button4)
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
            string operationDate = textBox1.Text.Trim();
            if (string.IsNullOrWhiteSpace(operationDate))
            {
                MessageBox.Show("Введите дату операции!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox1.Focus();
                return;
            }

            // Проверка формата даты (ГГГГ-ММ-ДД)
            string datePattern = @"^\d{4}-\d{2}-\d{2}$";
            if (!Regex.IsMatch(operationDate, datePattern))
            {
                MessageBox.Show("Дата операции должна быть в формате ГГГГ-ММ-ДД!\n" +
                    "Пример: 2024-01-15", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox1.Focus();
                return;
            }

            DateTime parsedDate;
            if (!DateTime.TryParseExact(operationDate, "yyyy-MM-dd", null,
                System.Globalization.DateTimeStyles.None, out parsedDate))
            {
                MessageBox.Show("Введите корректную дату операции!\nПример: 2024-01-15", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox1.Focus();
                return;
            }

            // ============= 2. ТИП ОПЕРАЦИИ (textBox2) =============
            string operationType = textBox2.Text.Trim();
            if (string.IsNullOrWhiteSpace(operationType))
            {
                operationType = null;
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

            // ============= 4. ПРОВЕРКА НАЛИЧИЯ ВЫБРАННЫХ ВРАЧЕЙ =============
            if (selectedDoctors.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы одного врача для операции!\n" +
                    "Используйте comboBox2 и кнопку 'Подтвердить выбор врачей'.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ============= 5. ДОБАВЛЕНИЕ ОПЕРАЦИИ =============
            try
            {
                // Начинаем транзакцию для согласованного добавления операции и врачей
                using (var transaction = Program.DatabaseConnection.BeginTransaction())
                {
                    // 5.1 Добавляем операцию
                    string insertOperation = @"
                        INSERT INTO operations (operation_date, operation_type, animal_id, operation_name)
                        VALUES (@operation_date, @operation_type, @animal_id, @operation_name);
                        SELECT last_insert_rowid();";

                    long newOperationId;
                    using (var cmd = new SQLiteCommand(insertOperation, Program.DatabaseConnection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@operation_date", operationDate);
                        cmd.Parameters.AddWithValue("@operation_type", operationType);
                        cmd.Parameters.AddWithValue("@animal_id", animalId);
                        cmd.Parameters.AddWithValue("@operation_name", "Операция");
                        newOperationId = (long)cmd.ExecuteScalar();
                    }

                    // 5.2 Добавляем связи операции с выбранными врачами
                    string insertOperationDoctor = @"
                        INSERT INTO operation_doctors (operation_id, doctor_id, role)
                        VALUES (@operation_id, @doctor_id, @role)";

                    foreach (int doctorId in selectedDoctors)
                    {
                        using (var cmd = new SQLiteCommand(insertOperationDoctor, Program.DatabaseConnection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@operation_id", newOperationId);
                            cmd.Parameters.AddWithValue("@doctor_id", doctorId);
                            cmd.Parameters.AddWithValue("@role", "Участвует");
                            cmd.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();

                    // Получаем имена выбранных врачей для сообщения
                    string doctorsList = "";
                    foreach (DataRow row in doctorsTable.Rows)
                    {
                        int docId = Convert.ToInt32(row["doctor_id"]);
                        if (selectedDoctors.Contains(docId))
                        {
                            doctorsList += $"• {row["full_name"]}\n";
                        }
                    }

                    MessageBox.Show($"✓ Операция успешно добавлена!\n\n" +
                        $"Дата операции: {operationDate}\n" +
                        $"Тип операции: {(string.IsNullOrEmpty(operationType) ? "не указан" : operationType)}\n" +
                        $"Животное: {animalName}\n" +
                        $"Участвующие врачи:\n{doctorsList}" +
                        $"ID операции: {newOperationId}",
                        "Успех",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Переход на Form11
                    Form11 form11 = new Form11();
                    this.Hide();
                    form11.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении операции: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Метод не используется, но нужен для конструктора
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Метод не используется, но нужен для конструктора
        }
    }
}
