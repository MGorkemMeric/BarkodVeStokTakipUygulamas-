using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
using BarkodStoksistemiApp;

namespace BarkodStoksistemiApp
{
    public partial class RegisterForm : Form
    {
        public RegisterForm()
        {
            InitializeComponent();
        }

        private void RegisterForm_Load(object sender, EventArgs e)
        {

        }

 

        private void btnRegister_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();
            string confirmPassword = txtPasswordConfirm.Text.Trim();

            if (string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("Lütfen bir e-posta adresi girin.");
                return;
            }

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Kullanıcı adı ve şifre boş olamaz.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("Şifreler uyuşmuyor!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string hashedPassword = SecurityHelper.ComputeSha256Hash(password); // Şifre hashleniyor

            using (var connection = new SqliteConnection(DatabaseHelper.GetConnectionString()))
            {
                connection.Open();

                var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = "SELECT COUNT(*) FROM Users WHERE Username = @username";
                checkCmd.Parameters.AddWithValue("@username", username);

                long existingUserCount = (long)checkCmd.ExecuteScalar();

                if (existingUserCount > 0)
                {
                    MessageBox.Show("Bu kullanıcı adı zaten kayıtlı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }


                var insertCmd = connection.CreateCommand();
                insertCmd.CommandText = "INSERT INTO Users (Username, Password, Email) VALUES (@username, @password, @Email)";
                insertCmd.Parameters.AddWithValue("@username", username);
                insertCmd.Parameters.AddWithValue("@password", hashedPassword);
                insertCmd.Parameters.AddWithValue("@Email", txtEmail.Text.Trim());
                insertCmd.ExecuteNonQuery();

                MessageBox.Show("Kayıt başarılı! Giriş yapabilirsiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close(); // Kayıt formunu kapat
            }
        }
    }
}