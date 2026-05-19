using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace med
{
    public partial class Form5 : Form
    {
        public Form5()
        {
            InitializeComponent();
            this.Load += Form5_Load;
            this.dataGridView1.CellClick += dataGridView1_CellClick;
        }

        private void Form5_Load(object sender, EventArgs e)
        {
            LoadAnimalsData();      // Животные в dataGridView1
        }

        // 1. Загрузка животных в dataGridView1
        private void LoadAnimalsData()
        {
            try
            {
                if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
                {
                    MessageBox.Show("Подключение к базе данных не установлено!");
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

                LoadVaccinationsData(animalId);  // Только дата вакцинации в dataGridView2
                LoadVaccinesData(animalId);      // Только название вакцины в dataGridView3
                LoadDoctorsData(animalId);       // Только врачи в dataGridView4
            }
        }

        // 2. Загрузка ТОЛЬКО ДАТЫ вакцинаций для выбранного животного в dataGridView2
        private void LoadVaccinationsData(int animalId)
        {
            try
            {
                if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
                {
                    return;
                }

                string sql = @"
                    SELECT 
                        vaccination_date AS 'Дата вакцинации'
                    FROM vaccinations 
                    WHERE animal_id = " + animalId + @"
                    ORDER BY vaccination_date DESC";

                using (var cmd = new SQLiteCommand(sql, Program.DatabaseConnection))
                {
                    DataTable dt = new DataTable();
                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }

                    if (dt.Rows.Count == 0)
                    {
                        DataTable emptyDt = new DataTable();
                        emptyDt.Columns.Add("Дата вакцинации", typeof(string));
                        emptyDt.Rows.Add("Нет вакцинаций");
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
                MessageBox.Show($"Ошибка загрузки дат вакцинаций: {ex.Message}");
            }
        }

        // 3. Загрузка ТОЛЬКО НАЗВАНИЯ вакцин для выбранного животного в dataGridView3
        private void LoadVaccinesData(int animalId)
        {
            try
            {
                if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
                {
                    return;
                }

                string sql = @"
                    SELECT DISTINCT 
                        vac.name AS 'Название вакцины'
                    FROM vaccinations v
                    INNER JOIN vaccines vac ON v.vaccine_id = vac.vaccine_id
                    WHERE v.animal_id = " + animalId;

                using (var cmd = new SQLiteCommand(sql, Program.DatabaseConnection))
                {
                    DataTable dt = new DataTable();
                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }

                    if (dt.Rows.Count == 0)
                    {
                        DataTable emptyDt = new DataTable();
                        emptyDt.Columns.Add("Название вакцины", typeof(string));
                        emptyDt.Rows.Add("Нет вакцин");
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
                MessageBox.Show($"Ошибка загрузки вакцин: {ex.Message}");
            }
        }

        // 4. Загрузка ВРАЧЕЙ для выбранного животного в dataGridView4 (с раздельными полями)
        private void LoadDoctorsData(int animalId)
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
                    FROM vaccinations v
                    INNER JOIN doctors d ON v.doctor_id = d.doctor_id
                    WHERE v.animal_id = " + animalId;

                using (var cmd = new SQLiteCommand(sql, Program.DatabaseConnection))
                {
                    DataTable dt = new DataTable();
                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }

                    if (dt.Rows.Count == 0)
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

        private void button3_Click(object sender, EventArgs e)
        {
            Form3 thirtForm = new Form3();
            this.Hide();
            thirtForm.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Form4 fourthForm = new Form4();
            this.Hide();
            fourthForm.Show();
        }

        private void dataGridView3_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}