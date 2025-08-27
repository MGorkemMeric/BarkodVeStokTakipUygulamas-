using System;
using System.Data;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace BarkodStoksistemiApp
{
    public partial class SaleDetailsForm : Form
    {
        private int _saleId;
        private decimal totalAmount = 0;
        private decimal paidAmount = 0;

        public SaleDetailsForm(int saleId)
        {
            InitializeComponent();
            _saleId = saleId;
            LoadSaleDetails();
            LoadPaymentInfo();
        }

        private void LoadSaleDetails()
        {
            string connectionString = DatabaseHelper.GetConnectionString();
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT p.Barcode AS 'Barkod',
                           p.Name AS 'Ürün Adı',
                           si.Quantity AS 'Adet',
                           si.PriceAtSale AS 'Birim Fiyat',
                           (si.Quantity * si.PriceAtSale) AS 'Toplam Fiyat',
                           si.SaleItemId
                    FROM SaleItems si
                    JOIN Products p ON si.ProductId = p.Id
                    WHERE si.SaleId = $saleId;
                ";
                cmd.Parameters.AddWithValue("$saleId", _saleId);

                using (var reader = cmd.ExecuteReader())
                {
                    DataTable dt = new DataTable();
                    dt.Load(reader);
                    dgvSaleDetails.DataSource = dt;
                    dgvSaleDetails.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                }
            }

            // Toplam satış miktarını al
            using (var connection = new SqliteConnection(DatabaseHelper.GetConnectionString()))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT TotalAmount FROM Sales WHERE SaleId = $saleId";
                cmd.Parameters.AddWithValue("$saleId", _saleId);
                totalAmount = Convert.ToDecimal(cmd.ExecuteScalar());
            }
        }

        private void LoadPaymentInfo()
        {
            using (var connection = new SqliteConnection(DatabaseHelper.GetConnectionString()))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT IFNULL(SUM(AmountPaid),0) FROM Payments WHERE SaleId = $saleId";
                cmd.Parameters.AddWithValue("$saleId", _saleId);
                paidAmount = Convert.ToDecimal(cmd.ExecuteScalar());
            }

            lblPaidAmount.Text = $"Ödenen: {paidAmount:C}";
            lblRemainingAmount.Text = $"Kalan: {totalAmount - paidAmount:C}";
        }

        private void btnAddPayment_Click(object sender, EventArgs e)
        {
            if (!decimal.TryParse(txtPaymentAmount.Text, out decimal payment))
            {
                MessageBox.Show("Geçerli bir miktar girin.");
                return;
            }

            if (payment <= 0)
            {
                MessageBox.Show("Miktar sıfırdan büyük olmalıdır.");
                return;
            }

            decimal remaining = totalAmount - paidAmount;
            if (payment > remaining)
            {
                MessageBox.Show("Girilen miktar kalan ödemeden fazla olamaz.");
                return;
            }

            using (var connection = new SqliteConnection(DatabaseHelper.GetConnectionString()))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Payments (SaleId, Amount, AmountPaid, PaymentMethod)
                    VALUES ($saleId, $amount, $amount, 'Veresiye')
                ";
                cmd.Parameters.AddWithValue("$saleId", _saleId);
                cmd.Parameters.AddWithValue("$amount", payment);
                cmd.ExecuteNonQuery();
            }

            LoadPaymentInfo();
            txtPaymentAmount.Clear();
        }

        private void btnDeleteProduct_Click(object sender, EventArgs e)
        {
            if (dgvSaleDetails.CurrentRow == null) return;

            int saleItemId = Convert.ToInt32(dgvSaleDetails.CurrentRow.Cells["SaleItemId"].Value);
            if (!int.TryParse(txtDeleteCount.Text, out int deleteCount) || deleteCount <= 0)
            {
                MessageBox.Show("Geçerli bir adet girin.");
                return;
            }

            int currentQuantity = Convert.ToInt32(dgvSaleDetails.CurrentRow.Cells["Adet"].Value);
            if (deleteCount > currentQuantity)
            {
                MessageBox.Show("İade edilecek miktar mevcut miktardan fazla olamaz.");
                return;
            }

            using (var connection = new SqliteConnection(DatabaseHelper.GetConnectionString()))
            {
                connection.Open();

                // Ürünün iade tutarını hesapla
                var cmdPrice = connection.CreateCommand();
                cmdPrice.CommandText = "SELECT PriceAtSale, ProductId FROM SaleItems WHERE SaleItemId = $saleItemId";
                cmdPrice.Parameters.AddWithValue("$saleItemId", saleItemId);

                int productId = 0;
                decimal unitPrice = 0;
                using (var reader = cmdPrice.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        unitPrice = reader.GetDecimal(0);
                        productId = reader.GetInt32(1);
                    }
                }

                decimal refundAmount = unitPrice * deleteCount;

                // SaleItems güncelleme veya silme
                if (deleteCount == currentQuantity)
                {
                    var cmdDelete = connection.CreateCommand();
                    cmdDelete.CommandText = "DELETE FROM SaleItems WHERE SaleItemId = $saleItemId";
                    cmdDelete.Parameters.AddWithValue("$saleItemId", saleItemId);
                    cmdDelete.ExecuteNonQuery();
                }
                else
                {
                    var cmdUpdate = connection.CreateCommand();
                    cmdUpdate.CommandText = "UPDATE SaleItems SET Quantity = Quantity - $count WHERE SaleItemId = $saleItemId";
                    cmdUpdate.Parameters.AddWithValue("$count", deleteCount);
                    cmdUpdate.Parameters.AddWithValue("$saleItemId", saleItemId);
                    cmdUpdate.ExecuteNonQuery();
                }

                // Ürünün stokunu geri ekle
                var cmdStock = connection.CreateCommand();
                cmdStock.CommandText = "UPDATE Products SET Stock = Stock + $count WHERE Id = $productId";
                cmdStock.Parameters.AddWithValue("$count", deleteCount);
                cmdStock.Parameters.AddWithValue("$productId", productId);
                cmdStock.ExecuteNonQuery();

                // Toplam satış miktarını güncelle
                var cmdTotal = connection.CreateCommand();
                cmdTotal.CommandText = "UPDATE Sales SET TotalAmount = TotalAmount - $refund WHERE SaleId = $saleId";
                cmdTotal.Parameters.AddWithValue("$refund", refundAmount);
                cmdTotal.Parameters.AddWithValue("$saleId", _saleId);
                cmdTotal.ExecuteNonQuery();

                // Ödeme kontrolü
                var cmdPaid = connection.CreateCommand();
                cmdPaid.CommandText = "SELECT IFNULL(SUM(AmountPaid),0) FROM Payments WHERE SaleId = $saleId";
                cmdPaid.Parameters.AddWithValue("$saleId", _saleId);
                decimal totalPaid = Convert.ToDecimal(cmdPaid.ExecuteScalar());

                decimal newTotalAmount;
                var cmdNewTotal = connection.CreateCommand();
                cmdNewTotal.CommandText = "SELECT TotalAmount FROM Sales WHERE SaleId = $saleId";
                cmdNewTotal.Parameters.AddWithValue("$saleId", _saleId);
                newTotalAmount = Convert.ToDecimal(cmdNewTotal.ExecuteScalar());

                // Ödenen fazla ise son ödemeden düşür
                if (totalPaid > newTotalAmount)
                {
                    decimal overpaid = totalPaid - newTotalAmount;

                    // Son ödeme ID'sini al
                    var cmdGetLastPayment = connection.CreateCommand();
                    cmdGetLastPayment.CommandText = "SELECT PaymentId, AmountPaid FROM Payments WHERE SaleId = $saleId ORDER BY PaymentId DESC LIMIT 1";
                    cmdGetLastPayment.Parameters.AddWithValue("$saleId", _saleId);

                    using (var reader = cmdGetLastPayment.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int paymentId = reader.GetInt32(0);
                            decimal currentPaid = reader.GetDecimal(1);
                            decimal newPaid = currentPaid - overpaid;
                            if (newPaid < 0) newPaid = 0;

                            var cmdUpdatePayment = connection.CreateCommand();
                            cmdUpdatePayment.CommandText = "UPDATE Payments SET AmountPaid = $newPaid WHERE PaymentId = $paymentId";
                            cmdUpdatePayment.Parameters.AddWithValue("$newPaid", newPaid);
                            cmdUpdatePayment.Parameters.AddWithValue("$paymentId", paymentId);
                            cmdUpdatePayment.ExecuteNonQuery();
                        }
                    }
                }

                // Kalan ödeme 0 ise kullanıcıya sor
                var cmdRemaining = connection.CreateCommand();
                cmdRemaining.CommandText = "SELECT TotalAmount - IFNULL((SELECT SUM(AmountPaid) FROM Payments WHERE SaleId = $saleId),0) FROM Sales WHERE SaleId = $saleId";
                cmdRemaining.Parameters.AddWithValue("$saleId", _saleId);
                decimal remaining = Convert.ToDecimal(cmdRemaining.ExecuteScalar());

                if (remaining <= 0)
                {
                    var confirm = MessageBox.Show(
                        "Tüm ödeme tamamlandı. Veresiye defterinden satışı silmek istiyor musunuz?",
                        "Ödeme Tamamlandı",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (confirm == DialogResult.Yes)
                    {
                        var cmdDeleteVeresiye = connection.CreateCommand();
                        cmdDeleteVeresiye.CommandText = "DELETE FROM Payments WHERE SaleId = $saleId";
                        cmdDeleteVeresiye.Parameters.AddWithValue("$saleId", _saleId);
                        cmdDeleteVeresiye.ExecuteNonQuery();

                        // Sales tablosunu silme, sadece veresiye formundan filtrelenebilir
                    }
                }
            }

            LoadSaleDetails();
            LoadPaymentInfo();
            txtDeleteCount.Clear();
        }
    }
}
