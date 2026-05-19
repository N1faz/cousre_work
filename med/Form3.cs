using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace med
{
    public partial class Form3 : Form
    {
        private int currentYear;

        public Form3()
        {
            InitializeComponent();
            this.Load += Form3_Load;
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            currentYear = DateTime.Now.Year;
            // Показываем текущий год в заголовке формы
            this.Text = $"Отчет о вакцинациях за {currentYear} год";
            // Загружаем отчет
            LoadVaccinationReport(currentYear);
        }

        // Процедура генерации отчета о вакцинациях за год
        private void LoadVaccinationReport(int year)
        {
            try
            {
                if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
                {
                    MessageBox.Show("Подключение к базе данных не установлено!", "Ошибка");
                    return;
                }

                // Создаем DataTable для отчета
                DataTable dt = new DataTable();
                dt.Columns.Add("ID", typeof(int));
                dt.Columns.Add("Дата вакцинации", typeof(string));
                dt.Columns.Add("Животное", typeof(string));
                dt.Columns.Add("Вакцина", typeof(string));
                dt.Columns.Add("Врач", typeof(string));
                dt.Columns.Add("Статус", typeof(string));

                // Получаем все вакцинации за год
                string sql = @"
                    SELECT 
                        vaccination_id,
                        vaccination_date,
                        animal_id,
                        vaccine_id,
                        doctor_id,
                        status
                    FROM vaccinations 
                    WHERE strftime('%Y', vaccination_date) = @year
                    ORDER BY vaccination_date DESC";

                using (var cmd = new SQLiteCommand(sql, Program.DatabaseConnection))
                {
                    cmd.Parameters.AddWithValue("@year", year.ToString());

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int vacId = reader.GetInt32(0);
                            string vacDate = reader.GetString(1);
                            int animalId = reader.GetInt32(2);
                            int vaccineId = reader.GetInt32(3);
                            int doctorId = reader.IsDBNull(4) ? -1 : reader.GetInt32(4);
                            string status = reader.GetString(5);

                            // Получаем имя животного
                            string animalName = "";
                            string sqlAnimal = "SELECT name FROM animals WHERE animal_id = " + animalId;
                            using (var cmd2 = new SQLiteCommand(sqlAnimal, Program.DatabaseConnection))
                            {
                                var result = cmd2.ExecuteScalar();
                                if (result != null) animalName = result.ToString();
                            }

                            // Получаем название вакцины
                            string vaccineName = "";
                            string sqlVaccine = "SELECT name FROM vaccines WHERE vaccine_id = " + vaccineId;
                            using (var cmd2 = new SQLiteCommand(sqlVaccine, Program.DatabaseConnection))
                            {
                                var result = cmd2.ExecuteScalar();
                                if (result != null) vaccineName = result.ToString();
                            }

                            // Получаем имя врача из раздельных полей last_name, first_name, middle_name
                            string doctorName = "Не указан";
                            if (doctorId != -1)
                            {
                                // Формируем ФИО врача из раздельных полей
                                string sqlDoctor = @"
                                    SELECT 
                                        last_name || ' ' || first_name || ' ' || COALESCE(middle_name, '') AS full_name 
                                    FROM doctors 
                                    WHERE doctor_id = " + doctorId;
                                using (var cmd2 = new SQLiteCommand(sqlDoctor, Program.DatabaseConnection))
                                {
                                    var result = cmd2.ExecuteScalar();
                                    if (result != null)
                                    {
                                        doctorName = result.ToString().Trim();
                                        if (string.IsNullOrWhiteSpace(doctorName))
                                            doctorName = "Не указан";
                                    }
                                }
                            }

                            // Добавляем строку в отчет
                            dt.Rows.Add(vacId, vacDate, animalName, vaccineName, doctorName, status);
                        }
                    }
                }

                // Выводим отчет в DataGridView
                dataGridView1.DataSource = dt;
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dataGridView1.ReadOnly = true;
                dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

                // Подсчет статистики
                int total = dt.Rows.Count;
                int completed = 0;
                int pending = 0;

                foreach (DataRow row in dt.Rows)
                {
                    string status = row["Статус"].ToString();
                    if (status == "completed") completed++;
                    else if (status == "pending") pending++;
                }

                // Обновляем заголовок формы со статистикой
                this.Text = $"Отчет о вакцинациях за {year} год | Всего: {total} | Завершено: {completed} | Запланировано: {pending}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при формировании отчета: {ex.Message}");
            }
        }

        // Кнопка для просмотра отчета за предыдущий год
        private void buttonPrevYear_Click(object sender, EventArgs e)
        {
            currentYear--;
            LoadVaccinationReport(currentYear);
            this.Text = $"Отчет о вакцинациях за {currentYear} год";
        }

        // Кнопка для просмотра отчета за следующий год
        private void buttonNextYear_Click(object sender, EventArgs e)
        {
            currentYear++;
            LoadVaccinationReport(currentYear);
            this.Text = $"Отчет о вакцинациях за {currentYear} год";
        }

        // Кнопка обновления отчета (за текущий год)
        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            currentYear = DateTime.Now.Year;
            LoadVaccinationReport(currentYear);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Form4 fourthForm = new Form4();
            this.Hide();
            fourthForm.Show();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
