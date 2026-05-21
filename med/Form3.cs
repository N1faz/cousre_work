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
            // Устанавливаем диапазон годов в numericUpDown1
            numericUpDown1.Minimum = 2000;
            numericUpDown1.Maximum = DateTime.Now.Year + 5;
            numericUpDown1.Value = DateTime.Now.Year;

            currentYear = DateTime.Now.Year;
            // Показываем текущий год в заголовке формы
            this.Text = $"Отчет о вакцинациях за {currentYear} год";
            // Загружаем отчет через VIEW
            LoadVaccinationReport(currentYear);
        }

        // Процедура генерации отчета о вакцинациях за год (через VIEW)
        private void LoadVaccinationReport(int year)
        {
            try
            {
                if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
                {
                    MessageBox.Show("Подключение к базе данных не установлено!", "Ошибка");
                    return;
                }

                // Используем VIEW vaccinations_report
                string sql = @"
                    SELECT 
                        vaccination_id AS 'ID',
                        vaccination_date AS 'Дата вакцинации',
                        animal_name AS 'Животное',
                        vaccine_name AS 'Вакцина',
                        doctor_name AS 'Врач',
                        status AS 'Статус'
                    FROM vaccinations_report 
                    WHERE year = @year
                    ORDER BY vaccination_date DESC";

                using (var cmd = new SQLiteCommand(sql, Program.DatabaseConnection))
                {
                    cmd.Parameters.AddWithValue("@year", year.ToString());

                    DataTable dt = new DataTable();
                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
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
            numericUpDown1.Value = currentYear;
            LoadVaccinationReport(currentYear);
            this.Text = $"Отчет о вакцинациях за {currentYear} год";
        }

        // Кнопка для просмотра отчета за следующий год
        private void buttonNextYear_Click(object sender, EventArgs e)
        {
            currentYear++;
            numericUpDown1.Value = currentYear;
            LoadVaccinationReport(currentYear);
            this.Text = $"Отчет о вакцинациях за {currentYear} год";
        }

        // Кнопка обновления отчета (за текущий год)
        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            currentYear = DateTime.Now.Year;
            numericUpDown1.Value = currentYear;
            LoadVaccinationReport(currentYear);
        }

        // Кнопка "Сформировать отчет" - считывает год из numericUpDown1
        private void button1_Click(object sender, EventArgs e)
        {
            currentYear = (int)numericUpDown1.Value;
            LoadVaccinationReport(currentYear);
            this.Text = $"Отчет о вакцинациях за {currentYear} год";
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

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {

        }
    }
}
