using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace med
{
    public partial class Form4 : Form
    {
        public Form4()
        {
            InitializeComponent();
            this.Load += Form4_Load;
        }

        private void Form4_Load(object sender, EventArgs e)
        {
            LoadAnimalsList();
        }

        // Загрузка списка животных через VIEW animals_search
        private void LoadAnimalsList()
        {
            try
            {
                if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
                {
                    MessageBox.Show("Подключение к базе данных не установлено!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Используем VIEW animals_search
                string sql = @"
                    SELECT 
                        animal_id AS 'ID',
                        animal_name AS 'Кличка',
                        age AS 'Возраст',
                        breed AS 'Порода',
                        species AS 'Вид'
                    FROM animals_search
                    ORDER BY animal_name";

                using (var cmd = new SQLiteCommand(sql, Program.DatabaseConnection))
                {
                    DataTable dt = new DataTable();
                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }

                    dataGridView1.DataSource = dt;
                    dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                    dataGridView1.ReadOnly = true;
                    dataGridView1.AllowUserToAddRows = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form5 fifthForm = new Form5();
            this.Hide();
            fifthForm.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Form6 sixthForm = new Form6();
            this.Hide();
            sixthForm.Show();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Form2 secondForm = new Form2();
            this.Hide();
            secondForm.Show();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Form7 seventhForm = new Form7();
            this.Hide();
            seventhForm.Show();
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow selectedRow = dataGridView1.Rows[e.RowIndex];

                int id = Convert.ToInt32(selectedRow.Cells["ID"].Value);
                string name = selectedRow.Cells["Кличка"].Value.ToString();

                Form1 form1 = new Form1(id, name);
                form1.Show();
            }
        }

        public void RefreshData()
        {
            LoadAnimalsList();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            // Поиск по виду животного при нажатии кнопки
            SearchBySpecies(textBox1.Text);
        }

        // Метод поиска по ВИДУ животного (species) с использованием VIEW animals_search
        private void SearchBySpecies(string searchText)
        {
            try
            {
                if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
                {
                    MessageBox.Show("Подключение к базе данных не установлено!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    LoadAllAnimals();
                    return;
                }

                // Используем VIEW animals_search для поиска по виду
                string sql = @"
                    SELECT 
                        animal_id AS 'ID',
                        animal_name AS 'Кличка',
                        age AS 'Возраст',
                        breed AS 'Порода',
                        species AS 'Вид'
                    FROM animals_search
                    WHERE LOWER(species) LIKE LOWER(@species)
                    ORDER BY animal_name";

                using (var cmd = new SQLiteCommand(sql, Program.DatabaseConnection))
                {
                    cmd.Parameters.AddWithValue("@species", $"%{searchText}%");

                    DataTable dt = new DataTable();
                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }

                    dataGridView1.DataSource = dt;
                    dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    dataGridView1.ReadOnly = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Метод для загрузки всех животных через VIEW animals_search
        private void LoadAllAnimals()
        {
            try
            {
                string sql = @"
                    SELECT 
                        animal_id AS 'ID',
                        animal_name AS 'Кличка',
                        age AS 'Возраст',
                        breed AS 'Порода',
                        species AS 'Вид'
                    FROM animals_search
                    ORDER BY animal_name";

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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        // Поиск по фамилии владельца (дополнительная функция)
        private void SearchByOwnerLastName(string searchText)
        {
            try
            {
                if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
                {
                    MessageBox.Show("Подключение к базе данных не установлено!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    LoadAllAnimals();
                    return;
                }

                // Поиск по фамилии владельца через VIEW animals_search
                string sql = @"
                    SELECT 
                        animal_id AS 'ID',
                        animal_name AS 'Кличка',
                        age AS 'Возраст',
                        breed AS 'Порода',
                        species AS 'Вид',
                        owner_last_name || ' ' || owner_first_name || ' ' || COALESCE(owner_middle_name, '') AS 'Владелец'
                    FROM animals_search
                    WHERE LOWER(owner_last_name) LIKE LOWER(@owner_last_name)
                    ORDER BY animal_name";

                using (var cmd = new SQLiteCommand(sql, Program.DatabaseConnection))
                {
                    cmd.Parameters.AddWithValue("@owner_last_name", $"%{searchText}%");

                    DataTable dt = new DataTable();
                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }

                    dataGridView1.DataSource = dt;
                    dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    dataGridView1.ReadOnly = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Кнопка "Показать всех" (очистить поиск)
        private void button6_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            LoadAllAnimals();
        }

        private void Form4_Load_1(object sender, EventArgs e)
        {

        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            // Проверка прав доступа (только для администратора)
            if (Program.CurrentUserRole != "admin")
            {
                MessageBox.Show("У вас нет прав для добавления вакцинаций!\n" +
                    "Эта функция доступна только администраторам.",
                    "Доступ запрещен",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Form11 newForm = new Form11();
            this.Hide();
            newForm.Show();
        }
    }
}