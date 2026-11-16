using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace _8
{
    public partial class Form1 : Form
    {
        
        private readonly string connectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=StudentDB;Integrated Security=True;";

        private DataTable _lastSnapshot;
        private int _selectedId = -1;

        public Form1()
        {
            InitializeComponent();

            
            this.Load += Form1_Load;
            dataGridViewStudents.SelectionChanged += dataGridViewStudents_SelectionChanged;
            buttonAdd.Click += buttonAdd_Click;
            buttonUpdate.Click += buttonUpdate_Click;
            buttonDelete.Click += buttonDelete_Click;
            buttonClear.Click += buttonClear_Click;
            buttonExport.Click += buttonExport_Click;
            buttonCancel.Click += buttonCancel_Click;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
               
                using (var da = new SqlDataAdapter(
                    "SELECT Id, FullName AS [ФИО], Age AS [Возраст], " +
                    "GroupName AS [Группа], AverageGrade AS [Средний балл] FROM Students ORDER BY Id", conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    dataGridViewStudents.DataSource = dt;
                    _lastSnapshot = dt.Copy();
                    UpdateStatus($"Загружено записей: {dt.Rows.Count}");
                }
                _selectedId = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dataGridViewStudents_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridViewStudents.CurrentRow == null) return;

            var row = dataGridViewStudents.CurrentRow;
            _selectedId = Convert.ToInt32(row.Cells["Id"].Value);
            textBoxName.Text = row.Cells["ФИО"].Value?.ToString();
            numericAge.Value = Convert.ToDecimal(row.Cells["Возраст"].Value);
            textBoxGroup.Text = row.Cells["Группа"].Value?.ToString();
            numericGrade.Value = Convert.ToDecimal(row.Cells["Средний балл"].Value);
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                if (StudentExists(textBoxName.Text.Trim()))
                {
                    MessageBox.Show("Студент с таким ФИО уже существует.", "Дубликат", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                   
                    string sql = @"INSERT INTO Students (FullName, Age, GroupName, AverageGrade)
                                   VALUES (@name, @age, @group, @grade)";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", textBoxName.Text.Trim());
                        cmd.Parameters.AddWithValue("@age", (int)numericAge.Value);
                        cmd.Parameters.AddWithValue("@group", textBoxGroup.Text.Trim());
                        cmd.Parameters.AddWithValue("@grade", numericGrade.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadData();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка добавления: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonUpdate_Click(object sender, EventArgs e)
        {
            if (_selectedId < 0)
            {
                MessageBox.Show("Выберите строку для редактирования.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!ValidateInput()) return;

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    
                    string sql = @"UPDATE Students
                                   SET FullName = @name, Age = @age,
                                       GroupName = @group, AverageGrade = @grade
                                   WHERE Id = @id";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", textBoxName.Text.Trim());
                        cmd.Parameters.AddWithValue("@age", (int)numericAge.Value);
                        cmd.Parameters.AddWithValue("@group", textBoxGroup.Text.Trim());
                        cmd.Parameters.AddWithValue("@grade", numericGrade.Value);
                        cmd.Parameters.AddWithValue("@id", _selectedId);
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка обновления: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            if (_selectedId < 0)
            {
                MessageBox.Show("Выберите строку для удаления.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show("Вы действительно хотите удалить выбранную запись?", "Подтверждение удаления",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("DELETE FROM Students WHERE Id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _selectedId);
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadData();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            ClearFields();
            _selectedId = -1;
            dataGridViewStudents.ClearSelection();
            UpdateStatus("Поля очищены.");
        }

        private void ClearFields()
        {
            textBoxName.Text = "";
            textBoxGroup.Text = "";
            numericAge.Value = 15;
            numericGrade.Value = 0;
        }

        private bool StudentExists(string fullName)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Students WHERE FullName = @name", conn))
                {
                    cmd.Parameters.AddWithValue("@name", fullName);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        private void buttonExport_Click(object sender, EventArgs e)
        {
            if (dataGridViewStudents.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта.");
                return;
            }

            using (var sfd = new SaveFileDialog { Filter = "CSV файл (*.csv)|*.csv" })
            {
                if (sfd.ShowDialog() != DialogResult.OK) return;

                try
                {
                    var sb = new StringBuilder();
                    var headers = new string[dataGridViewStudents.Columns.Count];
                    for (int i = 0; i < dataGridViewStudents.Columns.Count; i++)
                        headers[i] = dataGridViewStudents.Columns[i].HeaderText;
                    sb.AppendLine(string.Join(";", headers));

                    foreach (DataGridViewRow row in dataGridViewStudents.Rows)
                    {
                        if (row.IsNewRow) continue;
                        var cells = new string[dataGridViewStudents.Columns.Count];
                        for (int i = 0; i < dataGridViewStudents.Columns.Count; i++)
                            cells[i] = row.Cells[i].Value?.ToString() ?? "";
                        sb.AppendLine(string.Join(";", cells));
                    }

                    File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                    UpdateStatus("Экспортировано в " + sfd.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка экспорта: " + ex.Message);
                }
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            LoadData();
            UpdateStatus("Состояние обновлено из БД.");
        }

        private void UpdateStatus(string message)
        {
            labelStatus.Text = $"{message}  |  Всего студентов: {dataGridViewStudents.Rows.Count}";
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(textBoxName.Text))
            {
                MessageBox.Show("Поле 'ФИО' не может быть пустым.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBoxName.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(textBoxGroup.Text))
            {
                MessageBox.Show("Поле 'Группа' не может быть пустым.", "Валидация", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBoxGroup.Focus();
                return false;
            }
            return true;
        }
    }
}