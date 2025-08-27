using System;
using System.Data;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace BarkodStoksistemiApp
{
    public partial class SalesForm : Form
    {
        private DataTable salesDataTable;
        public SalesForm()
        {
            InitializeComponent();
        }

        private void SalesForm_Load(object sender, EventArgs e)
        {
            LoadSales();
        }

        public void LoadSales()
        {
            string connectionString = DatabaseHelper.GetConnectionString();

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT SaleId AS 'Satış No', 
                           SaleDate AS 'Tarih', 
                           PaymentMethod AS 'Ödeme Şekli', 
                           TotalAmount AS 'Toplam Tutar', 
                           Description AS 'Açıklama',
                           IsFullyPaid
                    FROM Sales
                    ORDER BY SaleDate DESC;
                ";

                using (var reader = cmd.ExecuteReader())
                {
                    salesDataTable = new DataTable();
                    salesDataTable.Load(reader);

                    dgvSales.AutoGenerateColumns = true;
                    dgvSales.DataSource = salesDataTable.DefaultView;
                    dgvSales.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                }
            }

            if (!dgvSales.Columns.Contains("Details"))
            {
                DataGridViewButtonColumn detailsButton = new DataGridViewButtonColumn
                {
                    Name = "Details",
                    HeaderText = "Satış Detayları",
                    Text = "Detayları Gör",
                    UseColumnTextForButtonValue = true
                };
                dgvSales.Columns.Add(detailsButton);
            }
        }

        private void dgvSales_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgvSales.Rows[e.RowIndex].IsNewRow) return;

            if (dgvSales.Columns[e.ColumnIndex].Name == "Details")
            {
                int saleId = Convert.ToInt32(dgvSales.Rows[e.RowIndex].Cells["Satış No"].Value);
                SaleDetailsForm detailsForm = new SaleDetailsForm(saleId);

                // Form kapandığında SalesForm güncellensin
                detailsForm.FormClosed += (s, args) => LoadSales();
                detailsForm.ShowDialog();
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            string filterText = txtSearch.Text.Replace("'", "''");

            if (string.IsNullOrWhiteSpace(filterText))
                salesDataTable.DefaultView.RowFilter = string.Empty;
            else
            {
                string filter = string.Format(
                    "Convert([Satış No], 'System.String') LIKE '%{0}%' OR " +
                    "Convert([Tarih], 'System.String') LIKE '%{0}%' OR " +
                    "[Ödeme Şekli] LIKE '%{0}%' OR " +
                    "Convert([Toplam Tutar], 'System.String') LIKE '%{0}%' OR " +
                    "[Açıklama] LIKE '%{0}%'", filterText);

                salesDataTable.DefaultView.RowFilter = filter;
            }
        }
    }
}
