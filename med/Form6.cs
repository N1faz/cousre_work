using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace med
{
    public partial class Form6 : Form
    {
        public Form6()
        {
            InitializeComponent();
            this.Load += Form6_Load;
            this.dataGridView1.CellClick += dataGridView1_CellClick;
        }

        private void Form6_Load(object sender, EventArgs e)
        {
            LoadAnimalsData(); // Загружаем список животных
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

        // Обработчик клика по dataGridView1 (выбор животного)
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow selectedRow = dataGridView1.Rows[e.RowIndex];

                // Получаем ID выбранного животного
                int animalId = Convert.ToInt32(selectedRow.Cells["animal_id"].Value);
                string animalName = selectedRow.Cells["Животное"].Value.ToString();

                // Загружаем информацию о выбранном животном
                LoadOperationTypesByAnimal(animalId);
                LoadOperationDatesByAnimal(animalId);
                LoadDoctorsByAnimal(animalId);
            }
        }

        // 2. Загрузка типов операций для выбранного животного (без JOIN)
        private void LoadOperationTypesByAnimal(int animalId)
        {
            try
            {
                if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
                {
                    return;
                }

                string sql = "SELECT DISTINCT operation_type AS 'Тип операции' FROM operations WHERE animal_id = " + animalId + " AND operation_type IS NOT NULL";

                using (var cmd = new SQLiteCommand(sql, Program.DatabaseConnection))
                {
                    DataTable dt = new DataTable();
                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                    dataGridView2.DataSource = dt;
                    dataGridView2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    dataGridView2.ReadOnly = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки типов операций: {ex.Message}");
            }
        }

        // 3. Загрузка дат операций для выбранного животного (без JOIN)
        private void LoadOperationDatesByAnimal(int animalId)
        {
            try
            {
                if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
                {
                    return;
                }

                string sql = "SELECT operation_date AS 'Дата операции' FROM operations WHERE animal_id = " + animalId + " ORDER BY operation_date DESC";

                using (var cmd = new SQLiteCommand(sql, Program.DatabaseConnection))
                {
                    DataTable dt = new DataTable();
                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                    dataGridView3.DataSource = dt;
                    dataGridView3.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    dataGridView3.ReadOnly = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки дат операций: {ex.Message}");
            }
        }

        // 4. Загрузка врачей для выбранного животного (с раздельными полями)
        private void LoadDoctorsByAnimal(int animalId)
        {
            try
            {
                if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
                {
                    return;
                }

                // Сначала получим все operation_id для данного животного
                string getOperationIds = "SELECT operation_id FROM operations WHERE animal_id = " + animalId;

                DataTable operationsDt = new DataTable();
                using (var cmd = new SQLiteCommand(getOperationIds, Program.DatabaseConnection))
                {
                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        adapter.Fill(operationsDt);
                    }
                }

                // Создаем DataTable для врачей
                DataTable doctorsDt = new DataTable();
                doctorsDt.Columns.Add("Врач", typeof(string));

                // Для каждой операции находим врачей
                foreach (DataRow row in operationsDt.Rows)
                {
                    int operationId = Convert.ToInt32(row["operation_id"]);

                    // Формируем ФИО врача из раздельных полей last_name, first_name, middle_name
                    string getDoctors = @"
                        SELECT 
                            d.last_name || ' ' || d.first_name || ' ' || COALESCE(d.middle_name, '') AS full_name 
                        FROM operation_doctors od 
                        INNER JOIN doctors d ON od.doctor_id = d.doctor_id 
                        WHERE od.operation_id = " + operationId;

                    using (var cmd = new SQLiteCommand(getDoctors, Program.DatabaseConnection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string doctorName = reader["full_name"].ToString().Trim();
                                // Добавляем только уникальных врачей
                                bool exists = false;
                                foreach (DataRow dr in doctorsDt.Rows)
                                {
                                    if (dr["Врач"].ToString() == doctorName)
                                    {
                                        exists = true;
                                        break;
                                    }
                                }
                                if (!exists && !string.IsNullOrWhiteSpace(doctorName))
                                {
                                    doctorsDt.Rows.Add(doctorName);
                                }
                            }
                        }
                    }
                }

                if (doctorsDt.Rows.Count == 0)
                {
                    doctorsDt.Rows.Add("Нет назначенных врачей");
                }

                dataGridView4.DataSource = doctorsDt;
                dataGridView4.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dataGridView4.ReadOnly = true;
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

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void dataGridView3_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}