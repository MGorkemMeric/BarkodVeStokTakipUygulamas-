using Microsoft.Data.Sqlite;
using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Xml.Linq;

namespace BarkodStoksistemiApp
{
    public partial class ProductDetailsForm : Form
    {

        private int productId;
        private Form1 _mainForm;

        public ProductDetailsForm(Form1 mainForm, int id, string barcode, string name, string price, string gelisFiyati, string stock, string description, string imagePath)
        {
            InitializeComponent();

            productId = id;
            _mainForm = mainForm;

            // TextBox'lara değerleri yerleştir
            txtBarcode.Text = barcode;
            txtProductName.Text = name;
            txtPrice.Text = price;
            txtPurchasePrice.Text = gelisFiyati;
            txtStock.Text = stock;
            txtDescription.Text = description;

            // Görseli yükle
            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                pictureBoxProductImage.Image = Image.FromFile(imagePath);
                pictureBoxProductImage.Tag = imagePath;
            }
            else
            {
                pictureBoxProductImage.Image = null;
                pictureBoxProductImage.Tag = null;
            }

            // DataTable oluştur
            DataTable dt = new DataTable();
            dt.Columns.Add("Barcode", typeof(string));
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Price", typeof(string));
            dt.Columns.Add("GelisFiyati", typeof(string));
            dt.Columns.Add("Stock", typeof(string));
            dt.Columns.Add("Description", typeof(string));
            dt.Columns.Add("ImagePath", typeof(string)); // gizlenecek

            // Satır ekle (ImagePath dahil)
            dt.Rows.Add(barcode, name, price, gelisFiyati, stock, description, imagePath);

            // DataGridView'a bağla
            dataGridViewProductDetails.DataSource = dt;

            // Başlıklar
            dataGridViewProductDetails.Columns["Barcode"].HeaderText = "Barkod";
            dataGridViewProductDetails.Columns["Name"].HeaderText = "Ürün Adı";
            dataGridViewProductDetails.Columns["Price"].HeaderText = "Ürün Fiyatı";
            dataGridViewProductDetails.Columns["GelisFiyati"].HeaderText = "Geliş Fiyatı";
            dataGridViewProductDetails.Columns["Stock"].HeaderText = "Stok Miktarı";
            dataGridViewProductDetails.Columns["Description"].HeaderText = "Açıklama";

            // ImagePath görünmesin
            dataGridViewProductDetails.Columns["ImagePath"].Visible = false;

            // İlk satırı seç
            if (dataGridViewProductDetails.Rows.Count > 0)
                dataGridViewProductDetails.Rows[0].Selected = true;

            // Veritabanından güncel veriyi çek
            RefreshProductDetails();
        }

        public void RefreshProductDetails()
        {
            using (var connection = new SQLiteConnection(DatabaseHelper.GetConnectionString()))
            {
                connection.Open();
                string query = "SELECT * FROM Products WHERE Id = @Id";
                SQLiteCommand command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@Id", productId);
                SQLiteDataAdapter adapter = new SQLiteDataAdapter(command);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                dataGridViewProductDetails.DataSource = dt;

                // Kolonları düzenle
                if (dataGridViewProductDetails.Columns.Contains("Id"))
                    dataGridViewProductDetails.Columns["Id"].Visible = false;
                if (dataGridViewProductDetails.Columns.Contains("ImagePath"))
                    dataGridViewProductDetails.Columns["ImagePath"].Visible = false;

                if (dataGridViewProductDetails.Columns.Contains("Barcode"))
                    dataGridViewProductDetails.Columns["Barcode"].HeaderText = "Barkod";
                if (dataGridViewProductDetails.Columns.Contains("Name"))
                    dataGridViewProductDetails.Columns["Name"].HeaderText = "Ürün Adı";
                if (dataGridViewProductDetails.Columns.Contains("Price"))
                    dataGridViewProductDetails.Columns["Price"].HeaderText = "Ürün Fiyatı";
                if (dataGridViewProductDetails.Columns.Contains("GelisFiyati"))
                    dataGridViewProductDetails.Columns["GelisFiyati"].HeaderText = "Geliş Fiyatı";
                if (dataGridViewProductDetails.Columns.Contains("Stock"))
                    dataGridViewProductDetails.Columns["Stock"].HeaderText = "Stok Miktarı";
                if (dataGridViewProductDetails.Columns.Contains("Description"))
                    dataGridViewProductDetails.Columns["Description"].HeaderText = "Açıklama";

                // TextBox ve PictureBox'ları güncelle
                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];

                    txtBarcode.Text = row["Barcode"]?.ToString();
                    txtProductName.Text = row["Name"]?.ToString();
                    txtPrice.Text = row["Price"]?.ToString();
                    txtPurchasePrice.Text = row["GelisFiyati"]?.ToString();
                    txtStock.Text = row["Stock"]?.ToString();
                    txtDescription.Text = row["Description"]?.ToString();

                    string imagePath = row["ImagePath"]?.ToString();
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        pictureBoxProductImage.Image = LoadImage(imagePath);
                        pictureBoxProductImage.Tag = imagePath;
                    }
                    else
                    {
                        pictureBoxProductImage.Image = null;
                        pictureBoxProductImage.Tag = null;
                    }
                }
            }
        }

        private Image LoadImage(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    return null;

                // Eğer path bir URL ise
                if (path.StartsWith("http://") || path.StartsWith("https://"))
                {
                    using (var client = new WebClient())
                    {
                        byte[] data = client.DownloadData(path);
                        using (var ms = new MemoryStream(data))
                        {
                            return new Bitmap(ms);
                        }
                    }
                }
                else if (File.Exists(path)) // Eğer lokal dosya ise
                {
                    return LoadImageNoLock(path);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Resim yüklenemedi: " + ex.Message);
            }

            return null;
        }

        private Image LoadImageNoLock(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                var img = Image.FromStream(fs);
                return new Bitmap(img);
            }
        }



        private void dataGridViewProductDetails_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridViewProductDetails.SelectedRows.Count == 0)
                return;

            var row = dataGridViewProductDetails.SelectedRows[0];

            txtBarcode.Text = row.Cells["Barcode"].Value?.ToString();
            txtProductName.Text = row.Cells["Name"].Value?.ToString();
            txtPrice.Text = row.Cells["Price"].Value?.ToString();
            txtPurchasePrice.Text = row.Cells["GelisFiyati"]?.Value?.ToString() ?? "";
            txtStock.Text = row.Cells["Stock"].Value?.ToString();
            txtDescription.Text = row.Cells["Description"].Value?.ToString();

            string imagePath = row.Cells["ImagePath"].Value?.ToString();

            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                // Önce varsa eski resmi dispose et
                if (pictureBoxProductImage.Image != null)
                {
                    pictureBoxProductImage.Image.Dispose();
                    pictureBoxProductImage.Image = null;
                }

                pictureBoxProductImage.Image = LoadImageNoLock(imagePath);
            }
            else
            {
                if (pictureBoxProductImage.Image != null)
                {
                    pictureBoxProductImage.Image.Dispose();
                    pictureBoxProductImage.Image = null;
                }
            }
        }









        private void buttonRemove_Click_1(object sender, EventArgs e)
        {
            if (pictureBoxProductImage.Image != null)
            {
                pictureBoxProductImage.Image.Dispose();
                pictureBoxProductImage.Image = null;
            }

            if (dataGridViewProductDetails.SelectedRows.Count > 0)
            {
                int id = Convert.ToInt32(dataGridViewProductDetails.SelectedRows[0].Cells["Id"].Value);

                using (var connection = new SQLiteConnection(DatabaseHelper.GetConnectionString()))
                {
                    connection.Open();
                    string query = "UPDATE Products SET ImagePath = NULL WHERE Id = @id";
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.ExecuteNonQuery();
                    }
                }

                // Veri ve UI güncelleme
                RefreshProductDetails();
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
                    var selectCommand = new SqliteCommand(selectQuery, connection);
                    selectCommand.Parameters.AddWithValue("$barcode", txtBarcode.Text);

                    string oldDetails = "";
                    string productName = "";

                    using (var reader = selectCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            productName = reader["Name"].ToString();
                            oldDetails = $"Name: {productName}, Price: {reader["Price"]}, Stock: {reader["Stock"]}, Description: {reader["Description"]}";
                        }
                        else
                        {
                            MessageBox.Show("Ürün bulunamadı.");
                            return;
                        }
                    }

                    // Ürünü sil
                    var deleteCommand = new SqliteCommand("DELETE FROM Products WHERE Barcode = $barcode", connection);
                    deleteCommand.Parameters.AddWithValue("$barcode", txtBarcode.Text);
                    int rowsAffected = deleteCommand.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // Log tablosuna silme işlemi için kayıt ekle
                        string logQuery = @"INSERT INTO Logs (Date, Action, Barcode, ProductName, OldDetails, NewDetails) 
                            VALUES (@Date, @Action, @Barcode, @ProductName, @OldDetails, @NewDetails)";
                        var logCommand = new SqliteCommand(logQuery, connection);
                        logCommand.Parameters.AddWithValue("@Date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        logCommand.Parameters.AddWithValue("@Action", "Silme");
                        logCommand.Parameters.AddWithValue("@Barcode", txtBarcode.Text);
                        logCommand.Parameters.AddWithValue("@ProductName", productName);
                        logCommand.Parameters.AddWithValue("@OldDetails", oldDetails);
                        logCommand.Parameters.AddWithValue("@NewDetails", ""); // Silme işleminde yeni detay yok
                        logCommand.ExecuteNonQuery();

                        MessageBox.Show("Ürün başarıyla silindi.");

                        // productId sıfırla, formu temizle
                        productId = 0;

                        txtBarcode.Clear();
                        txtProductName.Clear();
                        txtPrice.Clear();
                        txtStock.Clear();
                        txtDescription.Clear();
                        txtPurchasePrice.Clear();

                        // Güvenli şekilde RefreshProductDetails çağır
                        RefreshProductDetails();
                    }
                    else
                    {
                        MessageBox.Show("Ürün silinemedi.");
                    }
                }
            }
        }








        private void btnAddPhoto_Click(object sender, EventArgs e)
        {
            if (dataGridViewProductDetails.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen önce bir ürün seçin.");
                return;
            }

            string currentImage = dataGridViewProductDetails.SelectedRows[0].Cells["ImagePath"].Value?.ToString();
            if (!string.IsNullOrEmpty(currentImage) && File.Exists(currentImage))
            {
                DialogResult result = MessageBox.Show("Bu ürün için zaten bir fotoğraf mevcut. Değiştirmek istiyor musunuz?", "Fotoğraf Değiştir", MessageBoxButtons.YesNo);
                if (result == DialogResult.No) return;
            }

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Resim Dosyaları|*.jpg;*.jpeg;*.png;*.bmp";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedFile = openFileDialog.FileName;
                string imageFolder = Path.Combine(Application.StartupPath, "images");

                if (!Directory.Exists(imageFolder))
                    Directory.CreateDirectory(imageFolder);

                string newFileName = Guid.NewGuid() + Path.GetExtension(selectedFile);
                string newPath = Path.Combine(imageFolder, newFileName);
                File.Copy(selectedFile, newPath, true);

                using (SQLiteConnection connection = new SQLiteConnection(DatabaseHelper.GetConnectionString()))
                {
                    connection.Open();
                    string updateQuery = "UPDATE Products SET ImagePath = @ImagePath WHERE Id = @Id";
                    SQLiteCommand command = new SQLiteCommand(updateQuery, connection);
                    command.Parameters.AddWithValue("@ImagePath", newPath);
                    command.Parameters.AddWithValue("@Id", productId);
                    command.ExecuteNonQuery();
                }

                MessageBox.Show("Fotoğraf güncellendi.");

                // RefreshProductDetails ile tüm UI güncelleniyor
                RefreshProductDetails();

                // Eğer ekstra güncelleme istersek, fotoğrafı dispose edip yeniden yükle
                if (pictureBoxProductImage.Image != null)
                {
                    pictureBoxProductImage.Image.Dispose();
                    pictureBoxProductImage.Image = null;
                }
                pictureBoxProductImage.Image = LoadImageNoLock(newPath);
                pictureBoxProductImage.Tag = newPath;
            }
        }


        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (productId == 0)
            {
                MessageBox.Show("Lütfen güncellemek istediğiniz ürünü seçin.");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtBarcode.Text) ||
                string.IsNullOrWhiteSpace(txtProductName.Text) ||
                string.IsNullOrWhiteSpace(txtPrice.Text) ||
                string.IsNullOrWhiteSpace(txtStock.Text))
            {
                MessageBox.Show("Lütfen tüm alanları doldurun.");
                return;
            }

            // Eski veriler
            string oldBarcode = "", oldName = "", oldDescription = "";
            double oldPrice = 0;
            int oldStock = 0;
            double oldGelisFiyati = 0;

            using (SQLiteConnection conn = new SQLiteConnection(DatabaseHelper.GetConnectionString()))
            {
                conn.Open();
                string selectQuery = "SELECT * FROM Products WHERE Id=@Id";
                using (SQLiteCommand selectCommand = new SQLiteCommand(selectQuery, conn))
                {
                    selectCommand.Parameters.AddWithValue("@Id", productId);
                    using (SQLiteDataReader reader = selectCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            oldBarcode = reader["Barcode"].ToString();
                            oldName = reader["Name"].ToString();
                            oldDescription = reader["Description"].ToString();
                            double.TryParse(reader["Price"].ToString(), out oldPrice);
                            int.TryParse(reader["Stock"].ToString(), out oldStock);
                            oldGelisFiyati = reader["GelisFiyati"] == DBNull.Value ? 0 : Convert.ToDouble(reader["GelisFiyati"]);
                        }
                    }
                }
            }

            // Yeni veriler
            string newBarcode = txtBarcode.Text.Trim();
            string newName = txtProductName.Text.Trim();
            string newDescription = txtDescription.Text.Trim();

            if (!double.TryParse(txtPrice.Text.Trim(), out double newPrice))
            {
                MessageBox.Show("Geçerli bir fiyat giriniz.");
                return;
            }
            if (!int.TryParse(txtStock.Text.Trim(), out int newStock))
            {
                MessageBox.Show("Geçerli bir stok miktarı giriniz.");
                return;
            }
            double.TryParse(txtPurchasePrice.Text.Trim(), out double newGelisFiyati);

            // Güncelleme işlemi
            using (SQLiteConnection connection = new SQLiteConnection(DatabaseHelper.GetConnectionString()))
            {
                connection.Open();
                string updateQuery = @"
            UPDATE Products 
            SET Barcode=@Barcode, Name=@Name, Price=@Price, Stock=@Stock, Description=@Description, GelisFiyati=@GelisFiyati
            WHERE Id=@Id";
                using (SQLiteCommand command = new SQLiteCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@Barcode", newBarcode);
                    command.Parameters.AddWithValue("@Name", newName);
                    command.Parameters.AddWithValue("@Price", newPrice);
                    command.Parameters.AddWithValue("@Stock", newStock);
                    command.Parameters.AddWithValue("@Description", newDescription);
                    command.Parameters.AddWithValue("@GelisFiyati", newGelisFiyati);
                    command.Parameters.AddWithValue("@Id", productId);
                    command.ExecuteNonQuery();
                }

                // Log için ayrılmış detaylar
                string oldDetails = $"Name: {oldName}, Price: {oldPrice}, Stock: {oldStock}, Description: {oldDescription}, GelisFiyati: {oldGelisFiyati}";
                string newDetails = $"Name: {newName}, Price: {newPrice}, Stock: {newStock}, Description: {newDescription}, GelisFiyati: {newGelisFiyati}";

                string logQuery = "INSERT INTO Logs (Date, Action, Barcode, ProductName, OldDetails, NewDetails) VALUES (@Date, @Action, @Barcode, @ProductName, @OldDetails, @NewDetails)";
                using (SQLiteCommand logCommand = new SQLiteCommand(logQuery, connection))
                {
                    logCommand.Parameters.AddWithValue("@Date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    logCommand.Parameters.AddWithValue("@Action", "Güncelleme");
                    logCommand.Parameters.AddWithValue("@Barcode", newBarcode);
                    logCommand.Parameters.AddWithValue("@ProductName", newName);
                    logCommand.Parameters.AddWithValue("@OldDetails", oldDetails);
                    logCommand.Parameters.AddWithValue("@NewDetails", newDetails);
                    logCommand.ExecuteNonQuery();
                }
            }

            MessageBox.Show("Ürün başarıyla güncellendi.");
            _mainForm.Listele();
            RefreshProductDetails();
        }

        private void buttonRemove_Click(object sender, EventArgs e)
        {
            if (pictureBoxProductImage.Image != null)
            {
                pictureBoxProductImage.Image.Dispose();
                pictureBoxProductImage.Image = null;
            }

            if (dataGridViewProductDetails.SelectedRows.Count > 0)
            {
                int id = Convert.ToInt32(dataGridViewProductDetails.SelectedRows[0].Cells["Id"].Value);

                using (var connection = new SQLiteConnection(DatabaseHelper.GetConnectionString()))
                {
                    connection.Open();
                    string query = "UPDATE Products SET ImagePath = NULL WHERE Id = @id";
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.ExecuteNonQuery();
                    }
                }

                // Veritabanı güncellemesinden sonra formu güncelle
                RefreshProductDetails();
            }
        }

    }
}
