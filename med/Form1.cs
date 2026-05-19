using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace med
{
    public partial class Form1 : Form
    {
        private int _animalId;
        private string _animalName;

        public Form1()
        {
            InitializeComponent();
        }

        // Конструктор с параметрами (вызывается из Form4)
        public Form1(int animalId, string animalName)
        {
            InitializeComponent();
            _animalId = animalId;
            _animalName = animalName;
            this.Load += Form1_Load;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = $"Информация о животном: {_animalName}";
            LoadAllAnimalInfo();
        }

        // Вся информация о животном в одном DataGridView (без JOIN)
        private void LoadAllAnimalInfo()
        {
            try
            {
                if (Program.DatabaseConnection == null || Program.DatabaseConnection.State != ConnectionState.Open)
                {
                    MessageBox.Show("Подключение к базе данных не установлено!", "Ошибка");
                    return;
                }

                // Создаем DataTable вручную
                DataTable dt = new DataTable();

                // Добавляем колонки
                dt.Columns.Add("ID", typeof(int));
                dt.Columns.Add("Кличка", typeof(string));
                dt.Columns.Add("Возраст", typeof(int));
                dt.Columns.Add("Порода", typeof(string));
                dt.Columns.Add("Вид", typeof(string));
                dt.Columns.Add("Владелец", typeof(string));
                dt.Columns.Add("Телефон владельца", typeof(string));
                dt.Columns.Add("Дата выдачи паспорта", typeof(string));
                dt.Columns.Add("Страна выдачи", typeof(string));
                dt.Columns.Add("Вакцины", typeof(string));
                dt.Columns.Add("Кол-во приемов", typeof(int));
                dt.Columns.Add("Приемы", typeof(string));
                dt.Columns.Add("Кол-во вакцинаций", typeof(int));
                dt.Columns.Add("Вакцинации", typeof(string));
                dt.Columns.Add("Кол-во операций", typeof(int));
                dt.Columns.Add("Операции", typeof(string));

                // 1. Получаем информацию о животном
                string sqlAnimal = "SELECT animal_id, name, age, breed, species, owner_id FROM animals WHERE animal_id = " + _animalId;
                int ownerId = 0;
                string animalName = "", breed = "", species = "";
                int age = 0;

                using (var cmd = new SQLiteCommand(sqlAnimal, Program.DatabaseConnection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            ownerId = Convert.ToInt32(reader["owner_id"]);
                            animalName = reader["name"].ToString();
                            age = Convert.ToInt32(reader["age"]);
                            breed = reader["breed"].ToString();
                            species = reader["species"].ToString();
                        }
                    }
                }

                // 2. Получаем информацию о владельце (с раздельными полями)
                string ownerFullName = "", ownerPhone = "";
                string sqlOwner = "SELECT last_name, first_name, middle_name, phone FROM owners WHERE owner_id = " + ownerId;
                using (var cmd = new SQLiteCommand(sqlOwner, Program.DatabaseConnection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string lastName = reader["last_name"].ToString();
                            string firstName = reader["first_name"].ToString();
                            string middleName = reader["middle_name"]?.ToString() ?? "";
                            ownerFullName = $"{lastName} {firstName} {middleName}".Trim();
                            ownerPhone = reader["phone"].ToString();
                        }
                    }
                }

                // 3. Получаем информацию о ветпаспорте
                string passportDate = "", passportCountry = "";
                string sqlPassport = "SELECT issue_date, country_of_issue FROM vet_passports WHERE animal_id = " + _animalId;
                using (var cmd = new SQLiteCommand(sqlPassport, Program.DatabaseConnection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            passportDate = reader["issue_date"].ToString();
                            passportCountry = reader["country_of_issue"].ToString();
                        }
                    }
                }

                // 4. Получаем вакцины
                string vaccines = "";
                string sqlVaccine = @"
                    SELECT vac.name 
                    FROM vaccines vac 
                    WHERE vac.passport_id IN (SELECT passport_id FROM vet_passports WHERE animal_id = " + _animalId + ")";
                using (var cmd = new SQLiteCommand(sqlVaccine, Program.DatabaseConnection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        System.Collections.Generic.List<string> vaccineList = new System.Collections.Generic.List<string>();
                        while (reader.Read())
                        {
                            vaccineList.Add(reader["name"].ToString());
                        }
                        vaccines = string.Join(", ", vaccineList);
                    }
                }

                // 5. Получаем приемы
                int appointmentsCount = 0;
                string appointments = "";
                string sqlAppointments = "SELECT appointment_date, diagnosis FROM appointments WHERE animal_id = " + _animalId;
                using (var cmd = new SQLiteCommand(sqlAppointments, Program.DatabaseConnection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        System.Collections.Generic.List<string> appointmentList = new System.Collections.Generic.List<string>();
                        while (reader.Read())
                        {
                            appointmentsCount++;
                            string date = reader["appointment_date"].ToString();
                            string diagnosis = reader["diagnosis"].ToString();
                            appointmentList.Add($"{date} ({diagnosis})");
                        }
                        appointments = string.Join("; ", appointmentList);
                    }
                }

                // 6. Получаем вакцинации
                int vaccinationsCount = 0;
                string vaccinations = "";
                string sqlVaccinations = @"
                    SELECT vaccination_date, vaccine_id, status 
                    FROM vaccinations 
                    WHERE animal_id = " + _animalId;
                using (var cmd = new SQLiteCommand(sqlVaccinations, Program.DatabaseConnection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        System.Collections.Generic.List<string> vaccinationList = new System.Collections.Generic.List<string>();
                        while (reader.Read())
                        {
                            vaccinationsCount++;
                            string date = reader["vaccination_date"].ToString();
                            int vaccineId = Convert.ToInt32(reader["vaccine_id"]);
                            string status = reader["status"].ToString();

                            // Получаем название вакцины
                            string vaccineName = "";
                            string sqlVaccineName = "SELECT name FROM vaccines WHERE vaccine_id = " + vaccineId;
                            using (var cmd2 = new SQLiteCommand(sqlVaccineName, Program.DatabaseConnection))
                            {
                                var result = cmd2.ExecuteScalar();
                                if (result != null) vaccineName = result.ToString();
                            }
                            vaccinationList.Add($"{date} - {vaccineName} ({status})");
                        }
                        vaccinations = string.Join("; ", vaccinationList);
                    }
                }

                // 7. Получаем операции
                int operationsCount = 0;
                string operations = "";
                string sqlOperations = "SELECT operation_name, operation_date, operation_type FROM operations WHERE animal_id = " + _animalId;
                using (var cmd = new SQLiteCommand(sqlOperations, Program.DatabaseConnection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        System.Collections.Generic.List<string> operationList = new System.Collections.Generic.List<string>();
                        while (reader.Read())
                        {
                            operationsCount++;
                            string name = reader["operation_name"].ToString();
                            string date = reader["operation_date"].ToString();
                            string type = reader["operation_type"].ToString();
                            operationList.Add($"{name} ({date}) - {type}");
                        }
                        operations = string.Join("; ", operationList);
                    }
                }

                // Добавляем строку в DataTable
                dt.Rows.Add(
                    _animalId, animalName, age, breed, species, ownerFullName, ownerPhone,
                    passportDate, passportCountry, vaccines, appointmentsCount, appointments,
                    vaccinationsCount, vaccinations, operationsCount, operations
                );

                dataGridView1.DataSource = dt;
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dataGridView1.ReadOnly = true;
                dataGridView1.AllowUserToAddRows = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
            // Пустой метод
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