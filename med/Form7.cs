using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace med
{
    public partial class Form7 : Form
    {
        public Form7()
        {
            InitializeComponent();
            this.Load += Form7_Load;
            this.dataGridView1.CellClick += dataGridView1_CellClick;
        }

        private void Form7_Load(object sender, EventArgs e)
        {
            LoadAnimalsData();
            LoadTotalAppointmentsCount();
            textBox2.Text = "0"; // Начальное значение
        }

        // 1. Загрузка животных в dataGridView1
        private void LoadAnimalsData()
        {
            try
            {
                if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
                {
                    MessageBox.Show("Подключение к базе данных не установлено!", "Ошибка");
                    return;
                }

                string sql = "SELECT animal_id, name AS 'Животное' FROM animals ORDER BY name";
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

                    // Скрываем колонку animal_id
                    if (dataGridView1.Columns["animal_id"] != null)
                        dataGridView1.Columns["animal_id"].Visible = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки животных: {ex.Message}");
            }
        }

        // Обработчик клика по животному
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow selectedRow = dataGridView1.Rows[e.RowIndex];
                int animalId = Convert.ToInt32(selectedRow.Cells["animal_id"].Value);

                LoadDiagnosisData(animalId);           // Диагноз в dataGridView2
                LoadConsultationDateData(animalId);    // Дата консультации в dataGridView3
                LoadDoctorData(animalId);              // Врач в dataGridView4
                LoadSelectedAnimalAppointmentsCount(animalId); // Количество приемов в textBox2
            }
        }

        // Метод для подсчета количества приемов выбранного животного
        private void LoadSelectedAnimalAppointmentsCount(int animalId)
        {
            try
            {
                if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
                {
                    textBox2.Text = "Ошибка";
                    return;
                }

                string sql = "SELECT COUNT(*) FROM appointments WHERE animal_id = @animalId";
                using (var cmd = new SQLiteCommand(sql, Program.DatabaseConnection))
                {
                    cmd.Parameters.AddWithValue("@animalId", animalId);
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    textBox2.Text = count.ToString();
                }
            }
            catch (Exception ex)
            {
                textBox2.Text = "0";
            }
        }

        // 2. Загрузка диагнозов для выбранного животного в dataGridView2
        private void LoadDiagnosisData(int animalId)
        {
            try
            {
                if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
                {
                    return;
                }

                string sql = @"
                    SELECT 
                        diagnosis AS 'Диагноз'
                    FROM appointments 
                    WHERE animal_id = @animalId AND diagnosis IS NOT NULL AND diagnosis != ''
                    ORDER BY appointment_date DESC";

                using (var cmd = new SQLiteCommand(sql, Program.DatabaseConnection))
                {
                    cmd.Parameters.AddWithValue("@animalId", animalId);

                    DataTable dt = new DataTable();
                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }

                    if (dt.Rows.Count == 0)
                    {
                        DataTable emptyDt = new DataTable();
                        emptyDt.Columns.Add("Диагноз", typeof(string));
                        emptyDt.Rows.Add("Нет диагнозов");
                        dataGridView2.DataSource = emptyDt;
                    }
                    else
                    {
                        dataGridView2.DataSource = dt;
                    }

                    dataGridView2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    dataGridView2.ReadOnly = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки диагнозов: {ex.Message}");
            }
        }

        // 3. Загрузка дат консультаций для выбранного животного в dataGridView3
        private void LoadConsultationDateData(int animalId)
        {
            try
            {
                if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
                {
                    return;
                }

                string sql = @"
                    SELECT 
                        appointment_date AS 'Дата консультации'
                    FROM appointments 
                    WHERE animal_id = @animalId
                    ORDER BY appointment_date DESC";

                using (var cmd = new SQLiteCommand(sql, Program.DatabaseConnection))
                {
                    cmd.Parameters.AddWithValue("@animalId", animalId);

                    DataTable dt = new DataTable();
                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }

                    if (dt.Rows.Count == 0)
                    {
                        DataTable emptyDt = new DataTable();
                        emptyDt.Columns.Add("Дата консультации", typeof(string));
                        emptyDt.Rows.Add("Нет консультаций");
                        dataGridView3.DataSource = emptyDt;
                    }
                    else
                    {
                        dataGridView3.DataSource = dt;
                    }

                    dataGridView3.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    dataGridView3.ReadOnly = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки дат консультаций: {ex.Message}");
            }
        }

        // 4. Загрузка врачей для выбранного животного в dataGridView4 (с раздельными полями)
        private void LoadDoctorData(int animalId)
        {
            try
            {
                if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
                {
                    return;
                }

                // Формируем ФИО врача из раздельных полей last_name, first_name, middle_name
                string sql = @"
                    SELECT DISTINCT 
                        d.last_name || ' ' || d.first_name || ' ' || COALESCE(d.middle_name, '') AS 'Врач'
                    FROM appointments a
                    LEFT JOIN doctors d ON a.doctor_id = d.doctor_id
                    WHERE a.animal_id = @animalId";

                using (var cmd = new SQLiteCommand(sql, Program.DatabaseConnection))
                {
                    cmd.Parameters.AddWithValue("@animalId", animalId);

                    DataTable dt = new DataTable();
                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }

                    if (dt.Rows.Count == 0 || (dt.Rows.Count == 1 && string.IsNullOrWhiteSpace(dt.Rows[0]["Врач"].ToString())))
                    {
                        DataTable emptyDt = new DataTable();
                        emptyDt.Columns.Add("Врач", typeof(string));
                        emptyDt.Rows.Add("Нет назначенного врача");
                        dataGridView4.DataSource = emptyDt;
                    }
                    else
                    {
                        dataGridView4.DataSource = dt;
                    }

                    dataGridView4.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    dataGridView4.ReadOnly = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки врачей: {ex.Message}");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Form4 fourthForm = new Form4();
            this.Hide();
            fourthForm.Show();
        }

        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            LoadTotalAppointmentsCount();
        }

        // Метод для подсчета общего количества приемов
        private void LoadTotalAppointmentsCount()
        {
            try
            {
                if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
                {
                    textBox1.Text = "Ошибка подключения";
                    return;
                }

                string sql = "SELECT COUNT(*) FROM appointments";
                using (var cmd = new SQLiteCommand(sql, Program.DatabaseConnection))
                {
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    textBox1.Text = $"{count}";
                }
            }
            catch (Exception ex)
            {
                textBox1.Text = $"Ошибка: {ex.Message}";
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            // Этот метод вызывается при изменении текста в textBox2
            // Значение обновляется через LoadSelectedAnimalAppointmentsCount
        }
    }
}