using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
using BarkodStoksistemiApp.Properties;

namespace BarkodStoksistemiApp
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
            chkRememberMe.Checked = Properties.Settings.Default.RememberMe;

            if (chkRememberMe.Checked)
            {
                // Örneğin kayıtlı kullanıcı adını textbox’a yazdır
                txtUsername.Text = Properties.Settings.Default.Username;

            }
        }
        private void chkRememberMe_CheckedChanged(object sender, EventArgs e)
        {
            // "RememberMe" adında bir Settings ayarı olduğunu varsayalım (bool türünde)
            Properties.Settings.Default.RememberMe = chkRememberMe.Checked;
            Properties.Settings.Default.Save();
        }
        private void LoginForm_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.RememberMe)
            {
                txtUsername.Text = Properties.Settings.Default.Username;
                chkRememberMe.Checked = true;
            }
        }




        private void btnForgotPassword_Click_1(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Lütfen kullanıcı adınızı girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var connection = new SqliteConnection(DatabaseHelper.GetConnectionString()))

            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT Email FROM Users WHERE Username = @username";
                command.Parameters.AddWithValue("@username", username);

                object result = command.ExecuteScalar();

                if (result != null && result != DBNull.Value)
                {
                    string email = result.ToString();

                    // Email bulundu → şifre sıfırlama formunu aç
                    ForgotPasswordForm forgotForm = new ForgotPasswordForm(username, email);
                    forgotForm.Show(); // veya ShowDialog() eğer modal açılmasını istersen
                }
                else
                {
                    MessageBox.Show("Bu kullanıcı adına ait bir e-posta bulunamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            string hashedPassword = SecurityHelper.ComputeSha256Hash(password);

            using (var connection = new SqliteConnection(DatabaseHelper.GetConnectionString()))

            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM Users WHERE Username = @username AND Password = @password";
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@password", hashedPassword);

                long userCount = (long)command.ExecuteScalar();

                if (userCount > 0)
                {
                    if (chkRememberMe.Checked)
                    {
                        Properties.Settings.Default.Username = username;
                        Properties.Settings.Default.RememberMe = true;
                        Properties.Settings.Default.Save();
                    }
                    else
                    {
                        Properties.Settings.Default.Username = "";
                        Properties.Settings.Default.RememberMe = false;
                        Properties.Settings.Default.Save();
                    }

                    this.Hide();
                    Form1 mainForm = new Form1();
                    mainForm.Show();
                    mainForm.FormClosed += (s, args) => Application.Exit();
                }
                else
                {
                    MessageBox.Show("Kullanıcı adı veya şifre hatalı!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            RegisterForm registerForm = new RegisterForm();
            registerForm.ShowDialog();
        }
    }
}
