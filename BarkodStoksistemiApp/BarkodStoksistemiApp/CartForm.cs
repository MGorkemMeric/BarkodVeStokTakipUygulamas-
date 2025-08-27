using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace BarkodStoksistemiApp
{
    public partial class CartForm : Form
    {
        List<CartItem> cartItems;
        Form1 mainForm;


        public CartForm(List<CartItem> cartItems, Form1 form)
        {
            InitializeComponent();
            this.cartItems = cartItems;
            this.mainForm = form;
            LoadCart();
        }

        private void LoadCart()
        {
            cmbPaymentMethod.Items.Add("Nakit");
            cmbPaymentMethod.Items.Add("Kredi Kartı");
            cmbPaymentMethod.Items.Add("Havale");
            cmbPaymentMethod.Items.Add("Veresiye");
            cmbPaymentMethod.SelectedIndex = 0;  // Varsayılan seçim
            dgvCart.Rows.Clear();
            dgvCart.Columns.Clear();

            dgvCart.Columns.Add("Barcode", "Barcode");
            dgvCart.Columns.Add("Name", "Ürün Adı");
            dgvCart.Columns.Add("Price", "Birim Fiyat");
            dgvCart.Columns.Add("Quantity", "Adet");
            dgvCart.Columns.Add("Total", "Toplam Fiyat");

            // Sadece Quantity düzenlenemez yap (düzenlemeyi kaldırıyoruz)
            dgvCart.Columns["Quantity"].ReadOnly = true;

            dgvCart.Columns["Barcode"].ReadOnly = true;
            dgvCart.Columns["Name"].ReadOnly = true;
            dgvCart.Columns["Price"].ReadOnly = true;
            dgvCart.Columns["Total"].ReadOnly = true;

            foreach (var item in cartItems)
            {
                dgvCart.Rows.Add(item.Barcode, item.Name, item.Price.ToString("C2"), item.Quantity, (item.Price * item.Quantity).ToString("C2"));
            }

            lblTotal.Text = $"Toplam: {cartItems.Sum(x => x.Price * x.Quantity):C2}";
        }


        private int GetCurrentStockFromDatabase(int productId)
        {
            using (var connection = new SqliteConnection(DatabaseHelper.GetConnectionString()))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT Stock FROM Products WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", productId);
                var result = cmd.ExecuteScalar();
                return Convert.ToInt32(result);
            }
        }

        private void UpdateStockInDatabase(int productId, int newStock)
        {
            using (var connection = new SqliteConnection(DatabaseHelper.GetConnectionString()))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "UPDATE Products SET Stock = $stock WHERE Id = $id";
                cmd.Parameters.AddWithValue("$stock", newStock);
                cmd.Parameters.AddWithValue("$id", productId);
                cmd.ExecuteNonQuery();
            }
        }



        private void btnClearCart_Click(object sender, EventArgs e)
        {
            if (cartItems.Count > 0)
            {
                var result = MessageBox.Show("Sepeti tamamen temizlemek istediğinize emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    cartItems.Clear();
                    LoadCart();
                }
            }
            else
            {
                MessageBox.Show("Sepet zaten boş.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnRemoveFromCart_Click(object sender, EventArgs e)
        {
            if (dgvCart.SelectedRows.Count > 0)
            {
                string selectedBarcode = dgvCart.SelectedRows[0].Cells["Barcode"].Value.ToString();

                var selectedItem = cartItems.FirstOrDefault(x => x.Barcode == selectedBarcode);

                if (selectedItem != null)
                {
                    cartItems.Remove(selectedItem);

                    LoadCart();
                }
            }
            else
            {
                MessageBox.Show("Lütfen sepetten çıkarılacak bir ürün seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }


        private void btnCheckout_Click(object sender, EventArgs e)
        {
            if (cartItems.Count == 0)
            {
                MessageBox.Show("Sepet boş, satış yapılamaz.");
                return;
            }

            if (cmbPaymentMethod.SelectedItem == null)
            {
                MessageBox.Show("Lütfen ödeme yöntemini seçin.");
                return;
            }

            string paymentMethod = cmbPaymentMethod.SelectedItem.ToString();
            DateTime saleDate = DateTime.Now;
            decimal totalAmount = cartItems.Sum(item => (decimal)item.Price * item.Quantity);

            string saleDescription = txtSaleDescription.Text.Trim();

            using (var connection = new SqliteConnection(DatabaseHelper.GetConnectionString()))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Sales tablosuna satış ekle
                        var saleCommand = connection.CreateCommand();
                        saleCommand.Transaction = transaction;
                        saleCommand.CommandText = @"
                    INSERT INTO Sales (SaleDate, PaymentMethod, TotalAmount, Description)
                    VALUES ($saleDate, $paymentMethod, $totalAmount, $description);";
                        saleCommand.Parameters.AddWithValue("$saleDate", saleDate.ToString("yyyy-MM-dd HH:mm:ss"));
                        saleCommand.Parameters.AddWithValue("$paymentMethod", paymentMethod);
                        saleCommand.Parameters.AddWithValue("$totalAmount", totalAmount);
                        saleCommand.Parameters.AddWithValue("$description", saleDescription);
                        saleCommand.ExecuteNonQuery();

                        // 2. Son eklenen SaleId'yi al
                        var getIdCommand = connection.CreateCommand();
                        getIdCommand.Transaction = transaction;
                        getIdCommand.CommandText = "SELECT last_insert_rowid();";
                        long saleId = (long)getIdCommand.ExecuteScalar();

                        // 3. SaleItems tablosuna ürünleri ekle ve stok güncelle
                        foreach (var item in cartItems)
                        {
                            // Satış kalemini ekle
                            var itemCommand = connection.CreateCommand();
                            itemCommand.Transaction = transaction;
                            itemCommand.CommandText = @"
                        INSERT INTO SaleItems (SaleId, ProductId, Quantity, PriceAtSale)
                        VALUES ($saleId, $productId, $quantity, $priceAtSale);";
                            itemCommand.Parameters.AddWithValue("$saleId", saleId);
                            itemCommand.Parameters.AddWithValue("$productId", item.ProductId);
                            itemCommand.Parameters.AddWithValue("$quantity", item.Quantity);
                            itemCommand.Parameters.AddWithValue("$priceAtSale", item.Price);
                            itemCommand.ExecuteNonQuery();

                            // Mevcut stoğu al
                            var stockCmd = connection.CreateCommand();
                            stockCmd.Transaction = transaction;
                            stockCmd.CommandText = "SELECT Stock FROM Products WHERE Id = $id";
                            stockCmd.Parameters.AddWithValue("$id", item.ProductId);
                            int currentStock = Convert.ToInt32(stockCmd.ExecuteScalar());

                            // Yeni stoğu hesapla
                            int newStock = currentStock - item.Quantity;
                            if (newStock < 0) newStock = 0;

                            // Stok güncelle
                            var updateStockCmd = connection.CreateCommand();
                            updateStockCmd.Transaction = transaction;
                            updateStockCmd.CommandText = "UPDATE Products SET Stock = $stock WHERE Id = $id";
                            updateStockCmd.Parameters.AddWithValue("$stock", newStock);
                            updateStockCmd.Parameters.AddWithValue("$id", item.ProductId);
                            updateStockCmd.ExecuteNonQuery();

                            // Form1'de stok güncelle
                            mainForm.UpdateProductStock(item.ProductId, newStock);
                        }

                        transaction.Commit();
                        MessageBox.Show("Satış başarıyla tamamlandı");

                        txtSaleDescription.Clear();
                        cartItems.Clear();
                        LoadCart();
                        mainForm.Listele();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show("Satış kaydedilirken hata oluştu: " + ex.Message);
                    }
                }
            }
        }


    }
}
