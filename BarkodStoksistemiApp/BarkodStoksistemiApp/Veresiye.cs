using System;
using System.Data;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace BarkodStoksistemiApp
{
    public partial class Veresiye : Form
    {
        public Veresiye()
        {
            InitializeComponent();
            this.Load += Veresiye_Load;
            dvgVeresiye.CellContentClick += dvgVeresiye_CellContentClick;
        }

        private void Veresiye_Load(object sender, EventArgs e)
        {
            LoadVeresiye();
        }

        public void LoadVeresiye()
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
                    WHERE PaymentMethod = 'Veresiye' AND IsFullyPaid = 0
                    ORDER BY SaleDate DESC;
                ";

                using (var reader = cmd.ExecuteReader())
                {
                    DataTable dt = new DataTable();
                    dt.Load(reader);

                    dvgVeresiye.AutoGenerateColumns = true;
                    dvgVeresiye.DataSource = dt;
                    dvgVeresiye.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                }
            }

            // Buton sütunları
            if (!dvgVeresiye.Columns.Contains("Complete"))
            {
                DataGridViewButtonColumn completeButton = new DataGridViewButtonColumn
                {
                    Name = "Complete",
                    HeaderText = "İşlem",
                    Text = "Satışı Tamamla",
                    UseColumnTextForButtonValue = true
                };
                dvgVeresiye.Columns.Add(completeButton);
            }

            if (!dvgVeresiye.Columns.Contains("GoToSale"))
            {
                DataGridViewButtonColumn goToSaleButton = new DataGridViewButtonColumn
                {
                    Name = "GoToSale",
                    HeaderText = "Satışa Git",
                    Text = "Satışa Git",
                    UseColumnTextForButtonValue = true
                };
                dvgVeresiye.Columns.Add(goToSaleButton);
            }
        }

        private void dvgVeresiye_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dvgVeresiye.Rows[e.RowIndex].IsNewRow) return;

            string columnName = dvgVeresiye.Columns[e.ColumnIndex].Name;
            int saleId = Convert.ToInt32(dvgVeresiye.Rows[e.RowIndex].Cells["Satış No"].Value);

            if (columnName == "Complete")
            {
                var confirm = MessageBox.Show(
                    "Bu veresiye satışı tamamlamak istiyor musunuz?",
                    "Onay",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm == DialogResult.Yes)
                {
                    string connectionString = DatabaseHelper.GetConnectionString();
                    using (var connection = new SqliteConnection(connectionString))
                    {
                        connection.Open();

                        // Foreign key kısıtlamalarını aktif et
                        using (var pragmaCmd = connection.CreateCommand())
                        {
                            pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
                            pragmaCmd.ExecuteNonQuery();
                        }

                        using (var transaction = connection.BeginTransaction())
                        {
                            // Satışın toplam tutarını al
                            var totalCmd = connection.CreateCommand();
                            totalCmd.CommandText = "SELECT TotalAmount FROM Sales WHERE SaleId = $saleId";
                            totalCmd.Parameters.AddWithValue("$saleId", saleId);
                            decimal total = Convert.ToDecimal(totalCmd.ExecuteScalar());

                            // Ödenmiş miktarı al
                            var paidCmd = connection.CreateCommand();
                            paidCmd.CommandText = "SELECT IFNULL(SUM(AmountPaid),0) FROM Payments WHERE SaleId = $saleId";
                            paidCmd.Parameters.AddWithValue("$saleId", saleId);
                            decimal paid = Convert.ToDecimal(paidCmd.ExecuteScalar());

                            // Kalan miktarı ekle
                            decimal remaining = total - paid;
                            if (remaining > 0)
                            {
                                var addPaymentCmd = connection.CreateCommand();
                                addPaymentCmd.CommandText = @"
                                    INSERT INTO Payments (SaleId, Amount, AmountPaid, PaymentMethod)
                                    VALUES ($saleId, $amount, $amount, 'Veresiye')";
                                addPaymentCmd.Parameters.AddWithValue("$saleId", saleId);
                                addPaymentCmd.Parameters.AddWithValue("$amount", remaining);
                                addPaymentCmd.ExecuteNonQuery();
                            }

                            // Satışı tamamlandı olarak işaretle
                            var updateSaleCmd = connection.CreateCommand();
                            updateSaleCmd.CommandText = "UPDATE Sales SET IsFullyPaid = 1 WHERE SaleId = $saleId";
                            updateSaleCmd.Parameters.AddWithValue("$saleId", saleId);
                            updateSaleCmd.ExecuteNonQuery();

                            transaction.Commit();
                        }
                    }

                    // Veresiye DataGridView’i tekrar yükle
                    LoadVeresiye();
                }
            }
            else if (columnName == "GoToSale")
            {
                SaleDetailsForm detailsForm = new SaleDetailsForm(saleId);
                detailsForm.FormClosed += (s, args) => LoadVeresiye();
                detailsForm.ShowDialog();
            }
        }
    }
}
