using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
using ExcelDataReader;


namespace BarkodStoksistemiApp
{

    public partial class Form1 : Form
    {
        private System.Windows.Forms.Timer fadeInTimer;
        private System.Windows.Forms.Timer fadeOutTimer;
        private float labelOpacity = 0f;

        public Form1()
        {
            InitializeComponent();

            UrunEklendi.Visible = false;
            UrunEklendi.ForeColor = Color.FromArgb(0, UrunEklendi.ForeColor);

            fadeInTimer = new System.Windows.Forms.Timer();
            fadeInTimer.Interval = 50;
            fadeInTimer.Tick += FadeInTimer_Tick;

            fadeOutTimer = new System.Windows.Forms.Timer();
            fadeOutTimer.Interval = 50;
            fadeOutTimer.Tick += FadeOutTimer_Tick;
            dgvProducts.CellClick += dgvProducts_CellContentClick;
            this.Load += Form1_Load;
            dgvProducts.CellFormatting += dgvProducts_CellFormatting;
        }
        public List<CartItem> cartItems = new List<CartItem>();
        private void dgvProducts_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvProducts.Columns[e.ColumnIndex].Name == "Stock" && e.Value != null)
            {
                if (int.TryParse(e.Value.ToString(), out int stockValue))
                {
                    if (stockValue < 2)
                    {
                        dgvProducts.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.LightCoral;
                        dgvProducts.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.White;
                    }
                    else
                    {
                        dgvProducts.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.White;
                        dgvProducts.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.Black;
                    }
                }
            }
        }

        private void FadeInTimer_Tick(object sender, EventArgs e)
        {
            if (labelOpacity < 1f)
            {
                labelOpacity += 0.1f;
                int alpha = (int)(labelOpacity * 255);
                UrunEklendi.ForeColor = Color.FromArgb(alpha, UrunEklendi.ForeColor.R, UrunEklendi.ForeColor.G, UrunEklendi.ForeColor.B);
            }
            else
            {
                fadeInTimer.Stop();

                // 1.5 saniye bekleyip fadeOut başlat
                Task.Delay(1500).ContinueWith(_ => this.Invoke(new Action(() =>
                {
                    fadeOutTimer.Start();
                })));
            }
        }


        private void FadeOutTimer_Tick(object sender, EventArgs e)
        {
            if (labelOpacity > 0f)
            {
                labelOpacity -= 0.1f;
                if (labelOpacity < 0f) labelOpacity = 0f; // ❗ sınırla

                int alpha = (int)(labelOpacity * 255);
                UrunEklendi.ForeColor = Color.FromArgb(alpha, UrunEklendi.ForeColor.R, UrunEklendi.ForeColor.G, UrunEklendi.ForeColor.B);
            }
            else
            {
                fadeOutTimer.Stop();
                UrunEklendi.Visible = false;
            }
        }



        public void Listele()
        {
            using (var connection = new SqliteConnection(DatabaseHelper.GetConnectionString()))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Products"; // Güncel stokları çekiyor mu?
                using (var reader = command.ExecuteReader())
                {
                    DataTable dt = new DataTable();
                    dt.Load(reader);
                    dgvProducts.DataSource = dt;
                    if (dgvProducts.Columns.Contains("Barcode"))
                        dgvProducts.Columns["Barcode"].HeaderText = "Barkod";

                    if (dgvProducts.Columns.Contains("Name"))
                        dgvProducts.Columns["Name"].HeaderText = "Ürün Adı";

                    if (dgvProducts.Columns.Contains("Price"))
                        dgvProducts.Columns["Price"].HeaderText = "Satış Fiyatı";

                    if (dgvProducts.Columns.Contains("GelisFiyati"))
                        dgvProducts.Columns["GelisFiyati"].HeaderText = "Geliş (Alış) Fiyatı";

                    if (dgvProducts.Columns.Contains("Stock"))
                        dgvProducts.Columns["Stock"].HeaderText = "Stok";

                    if (dgvProducts.Columns.Contains("Description"))
                        dgvProducts.Columns["Description"].HeaderText = "Açıklama";

                    // Gerekirse Id ve ImagePath sütunlarını gizle
                    if (dgvProducts.Columns.Contains("ImagePath"))
                        dgvProducts.Columns["ImagePath"].Visible = false;
                    if (dgvProducts.Columns.Contains("Id"))
                        dgvProducts.Columns["Id"].Visible = false;
                }
            }
        }




        private void Form1_Load(object sender, EventArgs e)
        {
            CreateLogsTableIfNotExists();
            Listele();
            FillSearchComboBox();
            string dbPath = "Data Source=urunler.db";
            using (SQLiteConnection connection = new SQLiteConnection(DatabaseHelper.GetConnectionString()))
            {
                connection.Open();

                // ImagePath sütunu varsa ekleme, yoksa ekle
                using (SQLiteCommand command = new SQLiteCommand("PRAGMA table_info(Products)", connection))
                {
                    bool hasImagePath = false;
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader["name"].ToString() == "ImagePath")
                            {
                                hasImagePath = true;
                                break;
                            }
                        }
                    }

                    if (!hasImagePath)
                    {
                        using (SQLiteCommand alterCommand = new SQLiteCommand("ALTER TABLE Products ADD COLUMN ImagePath TEXT", connection))
                        {
                            alterCommand.ExecuteNonQuery();
                        }
                    }
                }

                connection.Close();
            }
            cmbSort.Items.Add("Fiyat (Artan)");
            cmbSort.Items.Add("Fiyat (Azalan)");
            cmbSort.Items.Add("Stok (Artan)");
            cmbSort.Items.Add("Stok (Azalan)");
            cmbSort.SelectedIndex = 0;

            using (var connection = new SqliteConnection(DatabaseHelper.GetConnectionString()))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
    CREATE TABLE IF NOT EXISTS Products (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Barcode TEXT,
        Name TEXT,
        Price REAL,
        Stock INTEGER,
        Description TEXT,
        ImagePath TEXT
    )";
                command.ExecuteNonQuery();
            }

        }



        public void UpdateProductStock(int productId, int newStock)
        {
            foreach (DataGridViewRow row in dgvProducts.Rows)
            {
                if (row.Cells["Id"].Value != null && Convert.ToInt32(row.Cells["Id"].Value) == productId)
                {
                    row.Cells["Stock"].Value = newStock;
                    break;
                }
            }
        }





        private void dgvProducts_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dgvProducts.Rows[e.RowIndex];
                txtBarcode.Text = row.Cells["Barcode"].Value?.ToString();
                txtName.Text = row.Cells["Name"].Value?.ToString();
                txtPrice.Text = row.Cells["Price"].Value?.ToString();
                txtStock.Text = row.Cells["Stock"].Value?.ToString();
                txtDescription.Text = row.Cells["Description"].Value?.ToString();
                txtPurchasePrice.Text = dgvProducts.CurrentRow.Cells["GelisFiyati"].Value?.ToString() ?? "";
            }
        }







        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBarcode.Text))
            {
                MessageBox.Show("Lütfen silmek istediğiniz ürünü listeden seçin.");
                return;
            }

            DialogResult result = MessageBox.Show("Seçilen ürünü silmek istediğinize emin misiniz?", "Onay", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                using (var connection = new SqliteConnection(DatabaseHelper.GetConnectionString()))
                {
                    connection.Open();

                    // Silinecek ürünün eski bilgilerini çek
                    string selectQuery = "SELECT * FROM Products WHERE Barcode = $barcode";
                    using (var selectCommand = new SqliteCommand(selectQuery, connection))
                    {
                        selectCommand.Parameters.AddWithValue("$barcode", txtBarcode.Text);

                        using (var reader = selectCommand.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                MessageBox.Show("Ürün bulunamadı.");
                                return;
                            }

                            // Eski detayları al
                            string oldName = reader["Name"].ToString();
                            string oldDescription = reader["Description"].ToString();
                            double oldPrice = Convert.ToDouble(reader["Price"]);
                            int oldStock = Convert.ToInt32(reader["Stock"]);

                            string oldDetails = $"Name: {oldName}, Price: {oldPrice}, Stock: {oldStock}, Description: {oldDescription}";

                            // Ürünü sil
                            string deleteQuery = "DELETE FROM Products WHERE Barcode = $barcode";
                            using (var deleteCommand = new SqliteCommand(deleteQuery, connection))
                            {
                                deleteCommand.Parameters.AddWithValue("$barcode", txtBarcode.Text);
                                int rowsAffected = deleteCommand.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    // Log tablosuna silme işlemi için kayıt ekle
                                    string logQuery = @"
                                INSERT INTO Logs (Date, Action, Barcode, ProductName, OldDetails, NewDetails)
                                VALUES ($date, $action, $barcode, $productName, $oldDetails, $newDetails);
                            ";
                                    using (var logCommand = new SqliteCommand(logQuery, connection))
                                    {
                                        logCommand.Parameters.AddWithValue("$date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                        logCommand.Parameters.AddWithValue("$action", "Silme");
                                        logCommand.Parameters.AddWithValue("$barcode", txtBarcode.Text);
                                        logCommand.Parameters.AddWithValue("$productName", oldName);
                                        logCommand.Parameters.AddWithValue("$oldDetails", oldDetails);
                                        logCommand.Parameters.AddWithValue("$newDetails", ""); // Silme işleminde yeni detay yok
                                        logCommand.ExecuteNonQuery();
                                    }

                                    MessageBox.Show("Ürün başarıyla silindi.");

                                    // Temizle
                                    txtBarcode.Clear();
                                    txtName.Clear();
                                    txtPrice.Clear();
                                    txtStock.Clear();
                                    txtDescription.Clear();

                                    Listele();
                                }
                                else
                                {
                                    MessageBox.Show("Ürün silinemedi.");
                                }
                            }
                        }
                    }
                }
            }
        }










        private void txtBarcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string barcode = txtBarcode.Text.Trim();

                if (string.IsNullOrEmpty(barcode))
                    return;

                using (var connection = new SqliteConnection(DatabaseHelper.GetConnectionString()))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT * FROM Products WHERE Barcode = $barcode";
                    command.Parameters.AddWithValue("$barcode", barcode);

                    using (var reader = command.ExecuteReader())
                    {
                        var table = new DataTable();
                        table.Load(reader);

                        if (table.Rows.Count > 0)
                        {
                            dgvProducts.DataSource = table;
                        }
                        else
                        {
                            MessageBox.Show("📦 Okutulan barkoda ait ürün bulunamadı!", "Ürün Bulunamadı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }

                e.SuppressKeyPress = true; // Enter tuşu sonrası beep sesini engeller
            }
        }







        private void CreateLogsTableIfNotExists()
        {
            using (var connection = new SQLiteConnection(DatabaseHelper.GetConnectionString()))
            {
                connection.Open();
                string createTableQuery = @"
            CREATE TABLE IF NOT EXISTS Logs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Date TEXT,
                Action TEXT,
                Barcode TEXT,
                ProductName TEXT,
                OldDetails TEXT,
                NewDetails TEXT
            );";
                using (var command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }



        // SEPETE EKLE KODULARI









        //Ekle butonu Kodları

        private void btnAdd_Click(object sender, EventArgs e)
        {
            // Boş alan kontrolü
            if (string.IsNullOrWhiteSpace(txtBarcode.Text) ||
                string.IsNullOrWhiteSpace(txtName.Text) ||
                string.IsNullOrWhiteSpace(txtPrice.Text) ||
                string.IsNullOrWhiteSpace(txtStock.Text) ||
                string.IsNullOrWhiteSpace(txtPurchasePrice.Text)) // Geliş fiyatı kontrolü
            {
                MessageBox.Show("Lütfen tüm alanları doldurun.");
                return;
            }

            // Geliş fiyatı kontrolü
            if (!double.TryParse(txtPurchasePrice.Text, out double gelisFiyati))
            {
                MessageBox.Show("Geliş fiyatı sayısal olmalıdır.");
                return;
            }
            if (gelisFiyati < 0)
            {
                MessageBox.Show("Geliş fiyatı negatif olamaz.");
                return;
            }

            // Fiyat ve stok kontrolü
            if (!double.TryParse(txtPrice.Text, out double price))
            {
                MessageBox.Show("Fiyat sayısal olmalıdır.");
                return;
            }

            if (!int.TryParse(txtStock.Text, out int stock))
            {
                MessageBox.Show("Stok sayısal bir değer olmalıdır.");
                return;
            }

            if (price < 0 || stock < 0)
            {
                MessageBox.Show("Fiyat ve stok negatif olamaz.");
                return;
            }

            using (var connection = new SqliteConnection(DatabaseHelper.GetConnectionString()))

            {
                connection.Open();

                // Aynı barkoddan var mı kontrolü
                var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = "SELECT COUNT(*) FROM Products WHERE Barcode = $barcode";
                checkCommand.Parameters.AddWithValue("$barcode", txtBarcode.Text);

                long count = (long)checkCommand.ExecuteScalar();
                if (count > 0)
                {
                    MessageBox.Show("Bu barkoda sahip bir ürün zaten mevcut. Lütfen farklı bir barkod girin.");
                    return;
                }

                // Ekleme işlemi
                var command = connection.CreateCommand();
                command.CommandText = @"
            INSERT INTO Products (Barcode, Name, Price, Stock, Description, GelisFiyati)
            VALUES ($barcode, $name, $price, $stock, $description, $gelisFiyati);
        ";
                command.Parameters.AddWithValue("$barcode", txtBarcode.Text);
                command.Parameters.AddWithValue("$name", txtName.Text);
                command.Parameters.AddWithValue("$price", price);
                command.Parameters.AddWithValue("$stock", stock);
                command.Parameters.AddWithValue("$description", txtDescription.Text);
                command.Parameters.AddWithValue("$gelisFiyati", gelisFiyati);
                command.ExecuteNonQuery();

                // Log ekleme
                string newDetails = $"Name: {txtName.Text}, Price: {price}, Stock: {stock}, Description: {txtDescription.Text}, GelisFiyati: {gelisFiyati}";
                var logCommand = connection.CreateCommand();
                logCommand.CommandText = @"
            INSERT INTO Logs (Date, Action, Barcode, ProductName, OldDetails, NewDetails)
            VALUES ($date, $action, $barcode, $productName, $oldDetails, $newDetails);
        ";
                logCommand.Parameters.AddWithValue("$date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                logCommand.Parameters.AddWithValue("$action", "Ekleme");
                logCommand.Parameters.AddWithValue("$barcode", txtBarcode.Text);
                logCommand.Parameters.AddWithValue("$productName", txtName.Text);
                logCommand.Parameters.AddWithValue("$oldDetails", "");
                logCommand.Parameters.AddWithValue("$newDetails", newDetails);
                logCommand.ExecuteNonQuery();
            }

            MessageBox.Show("Ürün başarıyla eklendi.");
            Listele();
        }

        private void btnSort_Click(object sender, EventArgs e)
        {
            string selectedSort = cmbSort.SelectedItem.ToString();
            string orderByClause = "";

            switch (selectedSort)
            {
                case "Fiyat (Artan)":
                    orderByClause = "ORDER BY Price ASC";
                    break;
                case "Fiyat (Azalan)":
                    orderByClause = "ORDER BY Price DESC";
                    break;
                case "Stok (Artan)":
                    orderByClause = "ORDER BY Stock ASC";
                    break;
                case "Stok (Azalan)":
                    orderByClause = "ORDER BY Stock DESC";
                    break;
            }

            using (var connection = new SqliteConnection(DatabaseHelper.GetConnectionString()))

            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Products " + orderByClause;

                using (var reader = command.ExecuteReader())
                {
                    var table = new System.Data.DataTable();
                    table.Load(reader);
                    dgvProducts.DataSource = table;
                }
            }
        }

        private void btnList_Click(object sender, EventArgs e)
        {
            using (var connection = new SqliteConnection(DatabaseHelper.GetConnectionString()))

            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Products";
                using (var reader = command.ExecuteReader())
                {
                    DataTable dt = new DataTable();
                    dt.Load(reader);
                    dgvProducts.DataSource = dt;
                    if (dgvProducts.Columns.Contains("ImagePath"))
                        dgvProducts.Columns["ImagePath"].Visible = false;
                    if (dgvProducts.Columns.Contains("Id"))
                        dgvProducts.Columns["Id"].Visible = false;

                }

                // Düşük stok kontrolü
                var checkLowStockCmd = connection.CreateCommand();
                checkLowStockCmd.CommandText = "SELECT COUNT(*) FROM Products WHERE Stock < 3";
                int lowStockCount = Convert.ToInt32(checkLowStockCmd.ExecuteScalar());

                if (lowStockCount > 0)
                {
                    MessageBox.Show("⚠ Düşük stoklu ürünleriniz mevcut!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (dgvProducts.CurrentRow != null)
            {
                int id = Convert.ToInt32(dgvProducts.CurrentRow.Cells["Id"].Value);

                using (var connection = new SqliteConnection(DatabaseHelper.GetConnectionString()))

                {
                    connection.Open();

                    // Eski verileri çek (GelisFiyati eklendi)
                    string oldBarcode = "", oldName = "", oldDescription = "";
                    double oldPrice = 0, oldGelisFiyati = 0;
                    int oldStock = 0;

                    using (var selectCommand = new SqliteCommand("SELECT * FROM Products WHERE Id = $id", connection))
                    {
                        selectCommand.Parameters.AddWithValue("$id", id);

                        using (var reader = selectCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                oldBarcode = reader["Barcode"].ToString();
                                oldName = reader["Name"].ToString();
                                oldDescription = reader["Description"].ToString();
                                oldPrice = Convert.ToDouble(reader["Price"]);
                                oldStock = Convert.ToInt32(reader["Stock"]);
                                oldGelisFiyati = reader["GelisFiyati"] == DBNull.Value ? 0 : Convert.ToDouble(reader["GelisFiyati"]);
                            }
                        }
                    }

                    // Yeni değerler (GelisFiyati için textbox veya numericupdown kontrolün olmalı)
                    string newBarcode = txtBarcode.Text;
                    string newName = txtName.Text;
                    string newDescription = txtDescription.Text;
                    double newPrice = Convert.ToDouble(txtPrice.Text);
                    int newStock = Convert.ToInt32(txtStock.Text);
                    double newGelisFiyati = string.IsNullOrWhiteSpace(txtPurchasePrice.Text) ? 0 : Convert.ToDouble(txtPurchasePrice.Text);

                    // Güncelleme sorgusu
                    var updateCommand = connection.CreateCommand();
                    updateCommand.CommandText = @"
                UPDATE Products SET 
                    Barcode = $barcode, 
                    Name = $name, 
                    Price = $price, 
                    Stock = $stock, 
                    Description = $description,
                    GelisFiyati = $gelisFiyati
                WHERE Id = $id;
            ";
                    updateCommand.Parameters.AddWithValue("$barcode", newBarcode);
                    updateCommand.Parameters.AddWithValue("$name", newName);
                    updateCommand.Parameters.AddWithValue("$price", newPrice);
                    updateCommand.Parameters.AddWithValue("$stock", newStock);
                    updateCommand.Parameters.AddWithValue("$description", newDescription);
                    updateCommand.Parameters.AddWithValue("$gelisFiyati", newGelisFiyati);
                    updateCommand.Parameters.AddWithValue("$id", id);

                    updateCommand.ExecuteNonQuery();

                    // Log ekle (GelisFiyati eklendi)
                    string oldDetails = $"Name: {oldName}, Price: {oldPrice}, Stock: {oldStock}, Description: {oldDescription}, GelisFiyati: {oldGelisFiyati}";
                    string newDetails = $"Name: {newName}, Price: {newPrice}, Stock: {newStock}, Description: {newDescription}, GelisFiyati: {newGelisFiyati}";

                    var logCommand = connection.CreateCommand();
                    logCommand.CommandText = @"
                INSERT INTO Logs (Date, Action, Barcode, ProductName, OldDetails, NewDetails)
                VALUES ($date, $action, $barcode, $productName, $oldDetails, $newDetails);
            ";
                    logCommand.Parameters.AddWithValue("$date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    logCommand.Parameters.AddWithValue("$action", "Güncelleme");
                    logCommand.Parameters.AddWithValue("$barcode", newBarcode);
                    logCommand.Parameters.AddWithValue("$productName", newName);
                    logCommand.Parameters.AddWithValue("$oldDetails", oldDetails);
                    logCommand.Parameters.AddWithValue("$newDetails", newDetails);

                    logCommand.ExecuteNonQuery();
                }

                MessageBox.Show("Ürün başarıyla güncellendi.");
                Listele(); // DataGridView'i tekrar yükle
            }
        }

        private void btnLowStock_Click(object sender, EventArgs e)
        {
            int stokLimiti = 2;

            using (var connection = new SqliteConnection(DatabaseHelper.GetConnectionString()))

            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Products WHERE Stock < $limit";
                command.Parameters.AddWithValue("$limit", stokLimiti);

                using (var reader = command.ExecuteReader())
                {
                    var table = new System.Data.DataTable();
                    table.Load(reader);

                    if (table.Rows.Count > 0)
                    {
                        dgvProducts.DataSource = table;
                        MessageBox.Show("Stok seviyesi düşük ürünler listelendi!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        MessageBox.Show("Tüm ürünlerin stoğu yeterli seviyede.");
                    }
                }
            }
        }

        private void btnAddToCart_Click(object sender, EventArgs e)
        {
            if (dgvProducts.CurrentRow != null)
            {
                string barcode = dgvProducts.CurrentRow.Cells["Barcode"].Value.ToString();
                string name = dgvProducts.CurrentRow.Cells["Name"].Value.ToString();
                double price = Convert.ToDouble(dgvProducts.CurrentRow.Cells["Price"].Value);
                int stock = Convert.ToInt32(dgvProducts.CurrentRow.Cells["Stock"].Value); // Stok miktarını alıyoruz

                int quantity = 1; // Varsayılan miktar

                if (!string.IsNullOrWhiteSpace(txtQuantity.Text))
                {
                    if (!int.TryParse(txtQuantity.Text, out quantity) || quantity < 1)
                    {
                        MessageBox.Show("Lütfen geçerli bir miktar girin (1 veya daha fazla).");
                        return;
                    }
                }

                var existingItem = cartItems.FirstOrDefault(x => x.Barcode == barcode);

                int totalQuantity = quantity;
                if (existingItem != null)
                {
                    totalQuantity += existingItem.Quantity;  // Sepette zaten varsa toplam miktarı hesapla
                }

                if (totalQuantity > stock)
                {
                    MessageBox.Show($"Stokta sadece {stock} adet '{name}' mevcut. Daha fazla ekleyemezsiniz.");
                    return;
                }

                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                }
                else
                {
                    int productId = Convert.ToInt32(dgvProducts.CurrentRow.Cells["Id"].Value);

                    cartItems.Add(new CartItem
                    {
                        ProductId = productId,
                        Barcode = barcode,
                        Name = name,
                        Price = price,
                        Quantity = quantity
                    });
                }

                // Label'a mesajı yaz
                UrunEklendi.Text = $"{quantity} adet '{name}' sepete eklendi.";
                UrunEklendi.Visible = true;

                fadeInTimer.Stop();
                fadeOutTimer.Stop();

                labelOpacity = 0f;
                UrunEklendi.ForeColor = Color.FromArgb(0, UrunEklendi.ForeColor.R, UrunEklendi.ForeColor.G, UrunEklendi.ForeColor.B);

                fadeInTimer.Start();
            }
            else
            {
                MessageBox.Show("Lütfen sepete eklemek için bir ürün seçin.");
            }
        }

        private void btnViewProduct_Click(object sender, EventArgs e)
        {
            if (dgvProducts.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen bir ürün seçin.");
                return;
            }

            var row = dgvProducts.SelectedRows[0];
            int id = Convert.ToInt32(row.Cells["Id"].Value);
            string barcode = row.Cells["Barcode"].Value.ToString();
            string name = row.Cells["Name"].Value.ToString();
            string price = row.Cells["Price"].Value.ToString();
            string purchasePrice = row.Cells["GelisFiyati"].Value?.ToString() ?? ""; // <- burası eklendi
            string stock = row.Cells["Stock"].Value.ToString();
            string description = row.Cells["Description"].Value.ToString();
            string imagePath = row.Cells["ImagePath"].Value?.ToString();


            var detailsForm = new ProductDetailsForm(this, id, barcode, name, price, purchasePrice, stock, description, imagePath ?? "");


            detailsForm.Show();
        }

        private void btnOpenCart_Click(object sender, EventArgs e)
        {
            CartForm cartForm = new CartForm(cartItems, this);
            cartForm.Show();
        }




        private void FillSearchComboBox()
        {
            cmbSearch.Items.Clear();
            cmbSearch.Items.Add("Genel"); // Genel arama seçeneği

            foreach (DataGridViewColumn col in dgvProducts.Columns)
            {
                if (col.Visible) // sadece kullanıcıya gösterilen kolonlar
                    cmbSearch.Items.Add(col.HeaderText);
            }

            cmbSearch.SelectedIndex = 0; // varsayılan Genel
        }




        private void btnSearch_Click(object sender, EventArgs e)
        {
            string searchText = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(searchText))
            {
                Listele(); // boş aramada tüm ürünleri göster
                return;
            }

            string selectedColumn = cmbSearch.SelectedItem?.ToString();
            List<string> columnsToSearch = new List<string>();

            if (selectedColumn == "Genel")
            {
                foreach (DataGridViewColumn col in dgvProducts.Columns)
                {
                    if (col.Visible)
                        columnsToSearch.Add(col.Name);
                }
            }
            else
            {
                var col = dgvProducts.Columns
                            .Cast<DataGridViewColumn>()
                            .FirstOrDefault(c => c.HeaderText == selectedColumn);
                if (col != null)
                    columnsToSearch.Add(col.Name);
            }

            string whereClause = string.Join(" OR ", columnsToSearch.Select(c => $"{c} LIKE $search"));
            string query = $"SELECT * FROM Products WHERE {whereClause}";

            using (var connection = new SqliteConnection(DatabaseHelper.GetConnectionString()))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = query;
                command.Parameters.AddWithValue("$search", "%" + searchText + "%");

                using (var reader = command.ExecuteReader())
                {
                    var table = new DataTable();
                    table.Load(reader);
                    dgvProducts.DataSource = table;
                }
            }
        }





        private void btnShowLogs_Click(object sender, EventArgs e)
        {
            LogForm logForm = new LogForm();
            logForm.ShowDialog();
        }

        private void btnSales_Click(object sender, EventArgs e)
        {
            SalesForm salesForm = new SalesForm();
            salesForm.ShowDialog();
        }

        private void btnRaporlar_Click(object sender, EventArgs e)
        {
            SalesReportForm raporForm = new SalesReportForm();
            raporForm.ShowDialog();
        }

        private void guna2GradientPanel1_Resize(object sender, EventArgs e)
        {
            guna2GradientPanel1.Left = (this.ClientSize.Width - guna2GradientPanel1.Width) / 2;
            guna2GradientPanel1.Top = (this.ClientSize.Height - guna2GradientPanel1.Height) / 2;
        }

        private void btnOnCredit_Click(object sender, EventArgs e)
        {
            Veresiye veresiyeForm = new Veresiye();
            veresiyeForm.ShowDialog(); // Modal olarak açılır
        }


    }
}
