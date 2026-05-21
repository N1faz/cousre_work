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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace med
{
    public partial class Form14 : Form
    {
        private int selectedDoctorId = -1; // ID выбранного врача
        private string selectedDoctorName = ""; // ФИО выбранного врача

        public Form14()
        {
            InitializeComponent();
            this.Load += Form14_Load;
            this.dataGridView1.CellClick += dataGridView1_CellClick;
        }

        private void Form14_Load(object sender, EventArgs e)
        {
            // Загружаем список врачей в dataGridView1
            LoadDoctorsToList();
        }

        // Загрузка списка врачей в dataGridView1
        private void LoadDoctorsToList()
        {
            try
            {
                if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
                {
                    MessageBox.Show("Подключение к базе данных не установлено!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string sql = @"
                    SELECT 
                        doctor_id AS 'ID',
                        last_name AS 'Фамилия',
                        first_name AS 'Имя',
                        COALESCE(middle_name, '') AS 'Отчество',
                        phone AS 'Телефон',
                        specialization AS 'Специальность',
                        experience_years AS 'Стаж',
                        COALESCE(status, 'работает') AS 'Статус'
                    FROM doctors
                    ORDER BY last_name";

                using (var cmd = new SQLiteCommand(sql, Program.DatabaseConnection))
                {
                    DataTable dt = new DataTable();
                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                    dataGridView1.DataSource = dt;
                    dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    dataGridView1.ReadOnly = true;
                    dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

                    // Скрываем колонку ID
                    if (dataGridView1.Columns["ID"] != null)
                        dataGridView1.Columns["ID"].Visible = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка врачей: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Клик по строке в dataGridView1 - выбор врача
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow selectedRow = dataGridView1.Rows[e.RowIndex];
                selectedDoctorId = Convert.ToInt32(selectedRow.Cells["ID"].Value);
                string lastName = selectedRow.Cells["Фамилия"].Value.ToString();
                string firstName = selectedRow.Cells["Имя"].Value.ToString();
                string middleName = selectedRow.Cells["Отчество"].Value.ToString();
                selectedDoctorName = $"{lastName} {firstName} {middleName}".Trim();

                // Подсвечиваем выбранную строку
                dataGridView1.ClearSelection();
                selectedRow.Selected = true;

                // Заполняем поля формы для редактирования/удаления
                textBox1.Text = lastName;
                textBox2.Text = firstName;
                textBox3.Text = middleName;
                textBox4.Text = selectedRow.Cells["Телефон"].Value.ToString();
                textBox5.Text = selectedRow.Cells["Специальность"].Value.ToString();
                textBox6.Text = selectedRow.Cells["Стаж"].Value.ToString();
            }
        }

        // Кнопка "Добавить врача" (button4)
        private void button4_Click(object sender, EventArgs e)
        {
            // Проверка подключения к базе данных
            if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
            {
                MessageBox.Show("Подключение к базе данных не установлено!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // ============= 1. ПРОВЕРКА ФАМИЛИИ (textBox1) =============
            string lastName = textBox1.Text.Trim();
            if (string.IsNullOrWhiteSpace(lastName))
            {
                MessageBox.Show("Введите фамилию врача!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox1.Focus();
                return;
            }

            // Проверка, что фамилия содержит только буквы и дефис
            if (!Regex.IsMatch(lastName, @"^[а-яА-Яa-zA-Z\-]+$"))
            {
                MessageBox.Show("Фамилия должна содержать только буквы и дефис!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox1.Focus();
                textBox1.SelectAll();
                return;
            }

            // ============= 2. ПРОВЕРКА ИМЕНИ (textBox2) =============
            string firstName = textBox2.Text.Trim();
            if (string.IsNullOrWhiteSpace(firstName))
            {
                MessageBox.Show("Введите имя врача!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox2.Focus();
                return;
            }

            // Проверка, что имя содержит только буквы
            if (!Regex.IsMatch(firstName, @"^[а-яА-Яa-zA-Z]+$"))
            {
                MessageBox.Show("Имя должно содержать только буквы!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox2.Focus();
                textBox2.SelectAll();
                return;
            }

            // ============= 3. ПРОВЕРКА ОТЧЕСТВА (textBox3) - НЕОБЯЗАТЕЛЬНОЕ ПОЛЕ =============
            string middleName = textBox3.Text.Trim();
            if (!string.IsNullOrWhiteSpace(middleName))
            {
                if (!Regex.IsMatch(middleName, @"^[а-яА-Яa-zA-Z\-]+$"))
                {
                    MessageBox.Show("Отчество должно содержать только буквы и дефис!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox3.Focus();
                    textBox3.SelectAll();
                    return;
                }
            }
            else
            {
                middleName = null;
            }

            // ============= 4. ПРОВЕРКА ТЕЛЕФОНА (textBox4) =============
            string phone = textBox4.Text.Trim();
            if (string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("Введите номер телефона врача!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox4.Focus();
                return;
            }

            // Проверка формата телефона (различные форматы)
            string phonePattern = @"^(\+?375|80)?\s*\(?\d{2}\)?\s*\d{3}\s*\d{2}\s*\d{2}$|^\+?[0-9]{10,15}$";
            if (!Regex.IsMatch(phone, phonePattern))
            {
                MessageBox.Show("Введите корректный номер телефона!\n" +
                    "Примеры: +375291234567, 80291234567, 291234567",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox4.Focus();
                textBox4.SelectAll();
                return;
            }

            // Приводим телефон к единому формату
            string cleanPhone = Regex.Replace(phone, @"[^\d\+]", "");
            if (!cleanPhone.StartsWith("+"))
            {
                if (cleanPhone.StartsWith("375"))
                    cleanPhone = "+" + cleanPhone;
                else if (cleanPhone.StartsWith("80"))
                    cleanPhone = "+375" + cleanPhone.Substring(2);
                else if (cleanPhone.Length == 9)
                    cleanPhone = "+375" + cleanPhone;
                else if (cleanPhone.Length == 12 && cleanPhone.StartsWith("375"))
                    cleanPhone = "+" + cleanPhone;
                else
                    cleanPhone = "+" + cleanPhone;
            }

            // ============= 5. ПРОВЕРКА СПЕЦИАЛЬНОСТИ (textBox5) =============
            string specialization = textBox5.Text.Trim();
            if (string.IsNullOrWhiteSpace(specialization))
            {
                MessageBox.Show("Введите специальность врача!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox5.Focus();
                return;
            }

            // ============= 6. ПРОВЕРКА СТАЖА (textBox6) =============
            string experienceText = textBox6.Text.Trim();
            if (string.IsNullOrWhiteSpace(experienceText))
            {
                MessageBox.Show("Введите стаж работы врача!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox6.Focus();
                return;
            }

            if (!int.TryParse(experienceText, out int experienceYears))
            {
                MessageBox.Show("Стаж должен быть целым числом!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox6.Focus();
                textBox6.SelectAll();
                return;
            }

            if (experienceYears < 0)
            {
                MessageBox.Show("Стаж не может быть отрицательным!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox6.Focus();
                textBox6.SelectAll();
                return;
            }

            if (experienceYears > 70)
            {
                MessageBox.Show("Стаж не может превышать 70 лет!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox6.Focus();
                textBox6.SelectAll();
                return;
            }

            // ============= 7. ПРОВЕРКА НА СУЩЕСТВОВАНИЕ ВРАЧА (дубликат) =============
            string checkSql = @"
                SELECT COUNT(*) FROM doctors 
                WHERE last_name = @last_name 
                  AND first_name = @first_name 
                  AND (middle_name = @middle_name OR (middle_name IS NULL AND @middle_name IS NULL))";

            using (var cmd = new SQLiteCommand(checkSql, Program.DatabaseConnection))
            {
                cmd.Parameters.AddWithValue("@last_name", lastName);
                cmd.Parameters.AddWithValue("@first_name", firstName);
                cmd.Parameters.AddWithValue("@middle_name", middleName);
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                if (count > 0)
                {
                    string fullName = $"{lastName} {firstName} {(string.IsNullOrEmpty(middleName) ? "" : middleName)}".Trim();
                    MessageBox.Show($"Врач '{fullName}' уже существует в базе данных!\n\n" +
                        "Повторное добавление того же врача не допускается.",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // ============= 8. ДОБАВЛЕНИЕ ВРАЧА =============
            try
            {
                string insertSql = @"
                    INSERT INTO doctors (last_name, first_name, middle_name, phone, specialization, experience_years, status)
                    VALUES (@last_name, @first_name, @middle_name, @phone, @specialization, @experience_years, 'работает');
                    SELECT last_insert_rowid();";

                long newDoctorId;
                using (var cmd = new SQLiteCommand(insertSql, Program.DatabaseConnection))
                {
                    cmd.Parameters.AddWithValue("@last_name", lastName);
                    cmd.Parameters.AddWithValue("@first_name", firstName);
                    cmd.Parameters.AddWithValue("@middle_name", middleName);
                    cmd.Parameters.AddWithValue("@phone", cleanPhone);
                    cmd.Parameters.AddWithValue("@specialization", specialization);
                    cmd.Parameters.AddWithValue("@experience_years", experienceYears);
                    newDoctorId = (long)cmd.ExecuteScalar();
                }

                string fullName = $"{lastName} {firstName} {(string.IsNullOrEmpty(middleName) ? "" : middleName)}".Trim();

                MessageBox.Show($"✓ Врач успешно добавлен!\n\n" +
                    $"ФИО: {fullName}\n" +
                    $"Телефон: {cleanPhone}\n" +
                    $"Специальность: {specialization}\n" +
                    $"Стаж: {experienceYears} лет\n" +
                    $"ID врача: {newDoctorId}",
                    "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Очищаем поля и обновляем список
                ClearFields();
                LoadDoctorsToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении врача: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Кнопка "Уволить врача" (button1) - обновление статуса на "уволен"
        private void button1_Click(object sender, EventArgs e)
        {
            // Проверка, выбран ли врач
            if (selectedDoctorId == -1)
            {
                MessageBox.Show("Выберите врача из списка для увольнения!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Проверка подключения к базе данных
            if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
            {
                MessageBox.Show("Подключение к базе данных не установлено!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Подтверждение увольнения
            DialogResult confirm = MessageBox.Show($"Вы уверены, что хотите уволить врача:\n\n{selectedDoctorName}?\n\n" +
                "Врач будет помечен как 'уволен'.",
                "Подтверждение увольнения",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm == DialogResult.No)
            {
                return;
            }

            try
            {
                string updateSql = "UPDATE doctors SET status = 'уволен' WHERE doctor_id = @doctor_id";
                using (var cmd = new SQLiteCommand(updateSql, Program.DatabaseConnection))
                {
                    cmd.Parameters.AddWithValue("@doctor_id", selectedDoctorId);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show($"✓ Врач '{selectedDoctorName}' успешно уволен (статус изменён на 'уволен')!", "Успех",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Обновляем список врачей
                        LoadDoctorsToList();

                        // Очищаем выбранного врача и поля
                        selectedDoctorId = -1;
                        selectedDoctorName = "";
                        ClearFields();
                    }
                    else
                    {
                        MessageBox.Show("Ошибка при увольнении врача!", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при увольнении врача: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Кнопка "Назад" (button2) - переход на Form4
        private void button2_Click(object sender, EventArgs e)
        {
            Form4 fourthForm = new Form4();
            this.Hide();
            fourthForm.Show();
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
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}